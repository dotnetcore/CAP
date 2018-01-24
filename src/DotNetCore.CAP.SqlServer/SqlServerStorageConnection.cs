using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;

        public SqlServerStorageConnection(SqlServerOptions options, CapOptions capOptions)
        {
            _capOptions = capOptions;
            Options = options;
        }

        public SqlServerOptions Options { get; }

        public IStorageTransaction CreateTransaction()
        {
            return new SqlServerStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM [{Options.Schema}].[Published] WITH (readpast) WHERE Id={id}";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public Task<IFetchedMessage> FetchNextMessageAsync()
        {
            var sql = $@"
DELETE TOP (1)
FROM [{Options.Schema}].[Queue] WITH (readpast, updlock, rowlock)
OUTPUT DELETED.MessageId,DELETED.[MessageType];";

            return FetchNextMessageCoreAsync(sql);
        }

        public async Task<CapPublishedMessage> GetNextPublishedMessageToBeEnqueuedAsync()
        {
            var sql =
                $"SELECT TOP (1) * FROM [{Options.Schema}].[Published] WITH (readpast) WHERE StatusName = '{StatusName.Scheduled}'";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetFailedPublishedMessages()
        {
            var sql =
                $"SELECT TOP (200) * FROM [{Options.Schema}].[Published] WITH (readpast) WHERE Retries<{_capOptions.FailedRetryCount} AND StatusName = '{StatusName.Failed}'";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $@"
INSERT INTO [{Options.Schema}].[Received]([Name],[Group],[Content],[Retries],[Added],[ExpiresAt],[StatusName])
VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.ExecuteAsync(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM [{Options.Schema}].[Received] WITH (readpast) WHERE Id={id}";
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<CapReceivedMessage> GetNextReceivedMessageToBeEnqueuedAsync()
        {
            var sql =
                $"SELECT TOP (1) * FROM [{Options.Schema}].[Received] WITH (readpast) WHERE StatusName = '{StatusName.Scheduled}'";
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetFailedReceivedMessages()
        {
            var sql =
                $"SELECT TOP (200) * FROM [{Options.Schema}].[Received] WITH (readpast) WHERE Retries<{_capOptions.FailedRetryCount} AND StatusName = '{StatusName.Failed}'";
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public bool ChangePublishedState(int messageId, string state)
        {
            var sql =
                $"UPDATE [{Options.Schema}].[Published] SET Retries=Retries+1,ExpiresAt=NULL,StatusName = '{state}' WHERE Id={messageId}";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public bool ChangeReceivedState(int messageId, string state)
        {
            var sql =
                $"UPDATE [{Options.Schema}].[Received] SET Retries=Retries+1,ExpiresAt=NULL,StatusName = '{state}' WHERE Id={messageId}";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public void Dispose()
        {
        }

        private async Task<IFetchedMessage> FetchNextMessageCoreAsync(string sql, object args = null)
        {
            //here don't use `using` to dispose
            var connection = new SqlConnection(Options.ConnectionString);
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            FetchedMessage fetchedMessage;
            try
            {
                fetchedMessage = await connection.QueryFirstOrDefaultAsync<FetchedMessage>(sql, args, transaction);
            }
            catch (SqlException)
            {
                transaction.Dispose();
                connection.Dispose();
                throw;
            }

            if (fetchedMessage == null)
            {
                transaction.Rollback();
                transaction.Dispose();
                connection.Dispose();
                return null;
            }

            return new SqlServerFetchedMessage(fetchedMessage.MessageId, fetchedMessage.MessageType, connection,
                transaction);
        }
    }
}
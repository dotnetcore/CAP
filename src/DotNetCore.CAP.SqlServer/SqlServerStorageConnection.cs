using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor.States;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerStorageConnection : IStorageConnection
    {
        private readonly SqlServerOptions _options;

        public SqlServerStorageConnection(SqlServerOptions options)
        {
            _options = options;
        }

        public SqlServerOptions Options => _options;

        public IStorageTransaction CreateTransaction()
        {
            return new SqlServerStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM [{_options.Schema}].[Published] WITH (readpast) WHERE Id={id}";

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public Task<IFetchedMessage> FetchNextMessageAsync()
        {
            var sql = $@"
DELETE TOP (1)
FROM [{_options.Schema}].[Queue] WITH (readpast, updlock, rowlock)
OUTPUT DELETED.MessageId,DELETED.[MessageType];";

            return FetchNextMessageCoreAsync(sql);
        }

        public async Task<CapPublishedMessage> GetNextPublishedMessageToBeEnqueuedAsync()
        {
            var sql = $"SELECT TOP (1) * FROM [{_options.Schema}].[Published] WITH (readpast) WHERE StatusName = '{StatusName.Scheduled}'";

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetFailedPublishedMessages()
        {
            var sql = $"SELECT * FROM [{_options.Schema}].[Published] WITH (readpast) WHERE StatusName = '{StatusName.Failed}'";

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        public bool ChangePublishedState(int messageId, IState state)
        {
            var sql = $"UPDATE [{_options.Schema}].[Published] SET Retries=Retries+1,StatusName = '{state.Name}' WHERE Id={messageId}";

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        // CapReceviedMessage

        public async Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $@"
INSERT INTO [{_options.Schema}].[Received]([Name],[Group],[Content],[Retries],[Added],[ExpiresAt],[StatusName])
VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                await connection.ExecuteAsync(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM [{_options.Schema}].[Received] WITH (readpast) WHERE Id={id}";
            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<CapReceivedMessage> GetNextReceviedMessageToBeEnqueuedAsync()
        {
            var sql = $"SELECT TOP (1) * FROM [{_options.Schema}].[Received] WITH (readpast) WHERE StatusName = '{StatusName.Scheduled}'";
            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetFailedReceviedMessages()
        {
            var sql = $"SELECT * FROM [{_options.Schema}].[Received] WITH (readpast) WHERE StatusName = '{StatusName.Failed}'";
            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public bool ChangeReceivedState(int messageId, IState state)
        {
            var sql = $"UPDATE [{_options.Schema}].[Received] SET Retries=Retries+1,StatusName = '{state.Name}' WHERE Id={messageId}";

            using (var connection = new SqlConnection(_options.ConnectionString))
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
            var connection = new SqlConnection(_options.ConnectionString);
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
                throw;
            }

            if (fetchedMessage == null)
            {
                transaction.Rollback();
                transaction.Dispose();
                connection.Dispose();
                return null;
            }

            return new SqlServerFetchedMessage(fetchedMessage.MessageId, fetchedMessage.MessageType, connection, transaction);
        }

        // ------------------------------------------

        public List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
        {
            return new List<string> { "11", "22", "33" };
        }
    }
}
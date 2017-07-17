using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore
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

        public Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM [{_options.Schema}].[Published] WITH (readpast) WHERE Id={id}";
            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
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

        // CapReceviedMessage

        public Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $@"
INSERT INTO [{_options.Schema}].[Received]([Name],[Group],[Content],[Retries],[Added],[ExpiresAt],[StatusName])
VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return connection.ExecuteAsync(sql, message);
            }
        }

        public Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM [{_options.Schema}].[Received] WITH (readpast) WHERE Id={id}";
            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                return connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
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

        public void Dispose()
        {
        }

        private async Task<IFetchedMessage> FetchNextMessageCoreAsync(string sql, object args = null)
        {
            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        var fetched = await connection.QueryFirstOrDefaultAsync<FetchedMessage>(sql, args, transaction);

                        if (fetched == null)
                            return null;

                        return new SqlServerFetchedMessage(fetched.MessageId, fetched.MessageType, connection, transaction);
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }
    }
}
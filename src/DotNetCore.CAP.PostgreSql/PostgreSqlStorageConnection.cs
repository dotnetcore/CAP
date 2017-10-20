using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;

        public PostgreSqlStorageConnection(PostgreSqlOptions options, CapOptions capOptions)
        {
            _capOptions = capOptions;
            Options = options;
        }

        public PostgreSqlOptions Options { get; }

        public IStorageTransaction CreateTransaction()
        {
            return new PostgreSqlStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var sql = $"SELECT * FROM \"{Options.Schema}\".\"published\" WHERE \"Id\"={id} FOR UPDATE SKIP LOCKED";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public Task<IFetchedMessage> FetchNextMessageAsync()
        {
            var sql = $@"DELETE FROM ""{Options.Schema}"".""queue"" WHERE ""MessageId"" = (SELECT ""MessageId"" FROM ""{Options.Schema}"".""queue"" FOR UPDATE SKIP LOCKED LIMIT 1) RETURNING *;";
            return FetchNextMessageCoreAsync(sql);
        }

        public async Task<CapPublishedMessage> GetNextPublishedMessageToBeEnqueuedAsync()
        {
            var sql =
                $"SELECT * FROM \"{Options.Schema}\".\"published\" WHERE \"StatusName\" = '{StatusName.Scheduled}' FOR UPDATE SKIP LOCKED LIMIT 1;";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetFailedPublishedMessages()
        {
            var sql =
                $"SELECT * FROM \"{Options.Schema}\".\"published\" WHERE \"Retries\"<{_capOptions.FailedRetryCount} AND \"StatusName\"='{StatusName.Failed}' LIMIT 200;";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql =
                $"INSERT INTO \"{Options.Schema}\".\"received\"(\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                await connection.ExecuteAsync(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var sql = $"SELECT * FROM \"{Options.Schema}\".\"received\" WHERE \"Id\"={id} FOR UPDATE SKIP LOCKED";
            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<CapReceivedMessage> GetNextReceivedMessageToBeEnqueuedAsync()
        {
            var sql =
                $"SELECT * FROM \"{Options.Schema}\".\"received\" WHERE \"StatusName\" = '{StatusName.Scheduled}' FOR UPDATE SKIP LOCKED LIMIT 1;";
            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetFailedReceivedMessages()
        {
            var sql =
                $"SELECT * FROM \"{Options.Schema}\".\"received\" WHERE \"Retries\"<{_capOptions.FailedRetryCount} AND \"StatusName\"='{StatusName.Failed}' LIMIT 200;";
            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public void Dispose()
        {
        }

        public bool ChangePublishedState(int messageId, string state)
        {
            var sql =
                $"UPDATE \"{Options.Schema}\".\"published\" SET \"Retries\"=\"Retries\"+1,\"StatusName\" = '{state}' WHERE \"Id\"={messageId}";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public bool ChangeReceivedState(int messageId, string state)
        {
            var sql =
                $"UPDATE \"{Options.Schema}\".\"received\" SET \"Retries\"=\"Retries\"+1,\"StatusName\" = '{state}' WHERE \"Id\"={messageId}";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        private async Task<IFetchedMessage> FetchNextMessageCoreAsync(string sql, object args = null)
        {
            //here don't use `using` to dispose
            var connection = new NpgsqlConnection(Options.ConnectionString);
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            FetchedMessage fetchedMessage;
            try
            {
                fetchedMessage = await connection.QueryFirstOrDefaultAsync<FetchedMessage>(sql, args, transaction);
            }
            catch (NpgsqlException)
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

            return new PostgreSqlFetchedMessage(fetchedMessage.MessageId, fetchedMessage.MessageType, connection,
                transaction);
        }
    }
}
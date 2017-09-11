using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor.States;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlStorageConnection : IStorageConnection
    {
        private readonly PostgreSqlOptions _options;

        public PostgreSqlStorageConnection(PostgreSqlOptions options)
        {
            _options = options;
        }

        public PostgreSqlOptions Options => _options;

        public IStorageTransaction CreateTransaction()
        {
            return new PostgreSqlStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var sql = $"SELECT * FROM \"{_options.Schema}\".\"published\" WHERE \"Id\"={id}";

            using (var connection = new NpgsqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public Task<IFetchedMessage> FetchNextMessageAsync()
        {
            var sql = $@"DELETE FROM ""{_options.Schema}"".""queue"" WHERE ""MessageId"" = (SELECT ""MessageId"" FROM ""{_options.Schema}"".""queue"" FOR UPDATE SKIP LOCKED LIMIT 1) RETURNING *;";
            return FetchNextMessageCoreAsync(sql);
        }

        public async Task<CapPublishedMessage> GetNextPublishedMessageToBeEnqueuedAsync()
        {
            var sql = $"SELECT * FROM \"{_options.Schema}\".\"published\" WHERE \"StatusName\" = '{StatusName.Scheduled}' FOR UPDATE SKIP LOCKED LIMIT 1;";

            using (var connection = new NpgsqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetFailedPublishedMessages()
        {
            var sql = $"SELECT * FROM \"{_options.Schema}\".\"published\" WHERE \"StatusName\"='{StatusName.Failed}' LIMIT 1000;";

            using (var connection = new NpgsqlConnection(_options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        // CapReceviedMessage

        public async Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $"INSERT INTO \"{_options.Schema}\".\"received\"(\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new NpgsqlConnection(_options.ConnectionString))
            {
                await connection.ExecuteAsync(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var sql = $"SELECT * FROM \"{_options.Schema}\".\"received\" WHERE \"Id\"={id}";
            using (var connection = new NpgsqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<CapReceivedMessage> GetNextReceviedMessageToBeEnqueuedAsync()
        {
            var sql = $"SELECT * FROM \"{_options.Schema}\".\"received\" WHERE \"StatusName\" = '{StatusName.Scheduled}' FOR UPDATE SKIP LOCKED LIMIT 1;";
            using (var connection = new NpgsqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetFailedReceviedMessages()
        {
            var sql = $"SELECT * FROM \"{_options.Schema}\".\"received\" WHERE \"StatusName\"='{StatusName.Failed}' LIMIT 1000;";
            using (var connection = new NpgsqlConnection(_options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public void Dispose()
        {
        }

        private async Task<IFetchedMessage> FetchNextMessageCoreAsync(string sql, object args = null)
        {
            //here don't use `using` to dispose
            var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            FetchedMessage fetchedMessage = null;
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

            return new PostgreSqlFetchedMessage(fetchedMessage.MessageId, fetchedMessage.MessageType, connection, transaction);
        }

        public List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
        {
            throw new NotImplementedException();
        }

        public bool ChangePublishedState(int messageId, IState state)
        {
            throw new NotImplementedException();
        }

        public bool ChangeReceivedState(int messageId, IState state)
        {
            throw new NotImplementedException();
        }
    }
}
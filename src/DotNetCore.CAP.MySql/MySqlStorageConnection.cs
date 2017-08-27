using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlStorageConnection : IStorageConnection
    {
        private readonly MySqlOptions _options;
        private readonly string _prefix;

        public MySqlStorageConnection(MySqlOptions options)
        {
            _options = options;
            _prefix = _options.TableNamePrefix;
        }

        public MySqlOptions Options => _options;

        public IStorageTransaction CreateTransaction()
        {
            return new MySqlStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM `{_prefix}.published` WHERE `Id`={id};";

            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public Task<IFetchedMessage> FetchNextMessageAsync()
        {
            //Last execute statement(FOR UPDATE to fix dirty read) :

            //SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
            //START TRANSACTION;
            //SELECT MessageId,MessageType FROM `{_prefix}.queue` LIMIT 1 FOR UPDATE;
            //DELETE FROM `{_prefix}.queue` LIMIT 1;
            //COMMIT;

            var sql = $@"
SELECT `MessageId`,`MessageType` FROM `{_prefix}.queue` LIMIT 1 FOR UPDATE;
DELETE FROM `{_prefix}.queue` LIMIT 1;";

            return FetchNextMessageCoreAsync(sql);
        }

        public async Task<CapPublishedMessage> GetNextPublishedMessageToBeEnqueuedAsync()
        {
            var sql = $"SELECT * FROM `{_prefix}.published` WHERE `StatusName` = '{StatusName.Scheduled}' LIMIT 1;";

            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetFailedPublishedMessages()
        {
            var sql = $"SELECT * FROM `{_prefix}.published` WHERE `StatusName` = '{StatusName.Failed}';";

            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        // CapReceviedMessage

        public async Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $@"
INSERT INTO `{_prefix}.received`(`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)
VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                await connection.ExecuteAsync(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM `{_prefix}.received` WHERE Id={id};";
            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<CapReceivedMessage> GetNextReceviedMessageToBeEnqueuedAsync()
        {
            var sql = $"SELECT * FROM `{_prefix}.received` WHERE `StatusName` = '{StatusName.Scheduled}' LIMIT 1;";
            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetFailedReceviedMessages()
        {
            var sql = $"SELECT * FROM `{_prefix}.received` WHERE `StatusName` = '{StatusName.Failed}';";
            using (var connection = new MySqlConnection(_options.ConnectionString))
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
            var connection = new MySqlConnection(_options.ConnectionString);
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            FetchedMessage fetchedMessage = null;
            try
            {
                fetchedMessage = await connection.QueryFirstOrDefaultAsync<FetchedMessage>(sql, args, transaction);
            }
            catch (MySqlException)
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

            return new MySqlFetchedMessage(fetchedMessage.MessageId, fetchedMessage.MessageType, connection, transaction);
        }

        public long GetSetCount(string key)
        {
            throw new NotImplementedException();
        }

        public List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
        {
            throw new NotImplementedException();
        }

        public MessageData GetJobData(string jobId)
        {
            throw new NotImplementedException();
        }

        public StateData GetStateData(string jobId)
        {
            throw new NotImplementedException();
        }
    }
}
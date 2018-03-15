using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;
        private readonly string _prefix;

        public MySqlStorageConnection(MySqlOptions options, CapOptions capOptions)
        {
            _capOptions = capOptions;
            Options = options;
            _prefix = Options.TableNamePrefix;
        }

        public MySqlOptions Options { get; }

        public IStorageTransaction CreateTransaction()
        {
            return new MySqlStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM `{_prefix}.published` WHERE `Id`={id};";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public Task<IFetchedMessage> FetchNextMessageAsync()
        {
            var processId = ObjectId.GenerateNewStringId();
            var sql = $@"
UPDATE `{_prefix}.queue` SET `ProcessId`=@ProcessId WHERE `ProcessId` IS NULL LIMIT 1;
SELECT `MessageId`,`MessageType` FROM `{_prefix}.queue` WHERE `ProcessId`=@ProcessId;";

            return FetchNextMessageCoreAsync(sql, processId);
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var sql = $"SELECT * FROM `{_prefix}.published` WHERE `Retries`<{_capOptions.FailedRetryCount} AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<int> StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $@"
INSERT INTO `{_prefix}.received`(`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)
VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);SELECT LAST_INSERT_ID();";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
               return await connection.ExecuteScalarAsync<int>(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM `{_prefix}.received` WHERE Id={id};";
            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var sql = $"SELECT * FROM `{_prefix}.received` WHERE `Retries`<{_capOptions.FailedRetryCount} AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";
            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public bool ChangePublishedState(int messageId, string state)
        {
            var sql =
                $"UPDATE `{_prefix}.published` SET `Retries`=`Retries`+1,`ExpiresAt`=NULL,`StatusName` = '{state}' WHERE `Id`={messageId}";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public bool ChangeReceivedState(int messageId, string state)
        {
            var sql =
                $"UPDATE `{_prefix}.received` SET `Retries`=`Retries`+1,`ExpiresAt`=NULL,`StatusName` = '{state}' WHERE `Id`={messageId}";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        private async Task<IFetchedMessage> FetchNextMessageCoreAsync(string sql, string processId)
        {
            FetchedMessage fetchedMessage;
            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                fetchedMessage = await connection.QuerySingleOrDefaultAsync<FetchedMessage>(sql, new { ProcessId = processId });
            }

            if (fetchedMessage == null)
                return null;

            return new MySqlFetchedMessage(fetchedMessage.MessageId, fetchedMessage.MessageType, processId, Options);
        }

        public void Dispose()
        {
        }
    }
}
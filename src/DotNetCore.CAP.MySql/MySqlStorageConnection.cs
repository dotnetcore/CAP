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
        private readonly CapOptions _capOptions;
        private readonly string _prefix;

        private const string DateTimeMaxValue = "9999-12-31 23:59:59";

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
            var sql = $@"
SELECT `MessageId`,`MessageType` FROM `{_prefix}.queue` LIMIT 1 FOR UPDATE;
DELETE FROM `{_prefix}.queue` LIMIT 1;";
            //            var sql = $@"
            //SELECT @MId:=`MessageId` as MessageId, @MType:=`MessageType` as MessageType FROM `{_prefix}.queue` LIMIT 1;
            //DELETE FROM `{_prefix}.queue` where `MessageId` = @MId AND `MessageType`=@MType;";

            return FetchNextMessageCoreAsync(sql);
        }

        public async Task<CapPublishedMessage> GetNextPublishedMessageToBeEnqueuedAsync()
        {
            var sql = $@"
UPDATE `{_prefix}.published` SET Id=LAST_INSERT_ID(Id),ExpiresAt='{DateTimeMaxValue}' WHERE ExpiresAt IS NULL AND `StatusName` = '{StatusName.Scheduled}' LIMIT 1;
SELECT * FROM `{_prefix}.published` WHERE Id=LAST_INSERT_ID();";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetFailedPublishedMessages()
        {
            var sql = $"SELECT * FROM `{_prefix}.published` WHERE `Retries`<{_capOptions.FailedRetryCount} AND `StatusName` = '{StatusName.Failed}' LIMIT 200;";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $@"
INSERT INTO `{_prefix}.received`(`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)
VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                await connection.ExecuteAsync(sql, message);
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

        public async Task<CapReceivedMessage> GetNextReceivedMessageToBeEnqueuedAsync()
        {
            var sql = $@"
UPDATE `{_prefix}.received` SET Id=LAST_INSERT_ID(Id),ExpiresAt='{DateTimeMaxValue}' WHERE ExpiresAt IS NULL AND `StatusName` = '{StatusName.Scheduled}' LIMIT 1;
SELECT * FROM `{_prefix}.received` WHERE Id=LAST_INSERT_ID();";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetFailedReceivedMessages()
        {
            var sql = $"SELECT * FROM `{_prefix}.received` WHERE `Retries`<{_capOptions.FailedRetryCount} AND `StatusName` = '{StatusName.Failed}' LIMIT 200;";
            using (var connection = new MySqlConnection(Options.ConnectionString))
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
                $"UPDATE `{_prefix}.published` SET `Retries`=`Retries`+1,`StatusName` = '{state}' WHERE `Id`={messageId}";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public bool ChangeReceivedState(int messageId, string state)
        {
            var sql =
                $"UPDATE `{_prefix}.received` SET `Retries`=`Retries`+1,`StatusName` = '{state}' WHERE `Id`={messageId}";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        private async Task<IFetchedMessage> FetchNextMessageCoreAsync(string sql, object args = null)
        {
            //here don't use `using` to dispose
            var connection = new MySqlConnection(Options.ConnectionString);
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            FetchedMessage fetchedMessage = null;
            try
            {
                //fetchedMessage = await connection.QuerySingleOrDefaultAsync<FetchedMessage>(sql, args, transaction);
                // An anomaly with unknown causes, sometimes QuerySingleOrDefaultAsync can't return expected result.
                using (var reader = connection.ExecuteReader(sql, args, transaction))
                {
                    while (reader.Read())
                    {
                        fetchedMessage = new FetchedMessage
                        {
                            MessageId = (int)reader.GetInt64(0),
                            MessageType = (MessageType)reader.GetInt64(1)
                        };
                    }
                }
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

            return new MySqlFetchedMessage(fetchedMessage.MessageId, fetchedMessage.MessageType, connection,
                transaction);
        }
    }
}
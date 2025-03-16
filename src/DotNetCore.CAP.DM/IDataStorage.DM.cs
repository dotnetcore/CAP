using Dm;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using System.Data.Common;


namespace DotNetCore.CAP.DM
{
    public class DMDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IStorageInitializer _initializer;
        private readonly string _lockName;
        private readonly IOptions<DMOptions> _options;
        private readonly string _pubName;
        private readonly string _recName;
        private readonly ISerializer _serializer;
        private readonly ISnowflakeId _snowflakeId;

        public DMDataStorage(
     IOptions<CapOptions> capOptions,
     IOptions<DMOptions> options,
     IStorageInitializer initializer,
     ISerializer serializer,
     ISnowflakeId snowflakeId)
        {
            _options = options;
            _initializer = initializer;
            _capOptions = capOptions;
            _serializer = serializer;
            _snowflakeId = snowflakeId;
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
            _lockName = initializer.GetLockTableName();
        }



        public async Task<bool> AcquireLockAsync(string key, TimeSpan ttl, string instance,
            CancellationToken token = default)
        {
            var sql =
                @$"UPDATE {_lockName} SET ""Instance"" = :Instance, ""LastLockTime"" = :LastLockTime WHERE ""Key"" = :Key AND ""LastLockTime"" < :TTL";
            var connection = new DmConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
            new DmParameter("Instance", instance),
            new DmParameter("LastLockTime", DateTime.Now){ DmSqlType = DmDbType.Date},
            new DmParameter("Key", key),
            new DmParameter("TTL", DateTime.Now.Subtract(ttl)){ DmSqlType = DmDbType.Date}
        };
            var opResult = await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
            return opResult > 0;
        }

        public async Task ReleaseLockAsync(string key, string instance, CancellationToken cancellationToken = default)
        {
            var sql =
                $@"UPDATE {_lockName} SET ""Instance"" = '', ""LastLockTime"" = :LastLockTime WHERE ""Key"" = :Key AND ""Instance"" = :Instance";
            var connection = new DmConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
            new DmParameter("LastLockTime", DateTime.MinValue) { DmSqlType = DmDbType.Date },
            new DmParameter("Key", key),
            new DmParameter("Instance", instance),
        };
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        public async Task RenewLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
        {
            var sql =
                $@"UPDATE {_lockName} SET ""LastLockTime"" = ""LastLockTime"" + ({ttl.TotalSeconds} / (24 * 60 * 60)) WHERE ""Key"" = :Key AND ""Instance"" = :Instance
";
            var connection = new DmConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
            new DmParameter("Key", key),
            new DmParameter("Instance", instance)
        };
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        public async Task ChangePublishStateToDelayedAsync(string[] ids)
        {
            var sql = $@"UPDATE {_pubName} SET ""StatusName"" = '{StatusName.Delayed}' WHERE ""Id"" IN ({string.Join(',', ids)});";
            var connection = new DmConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }

        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state, object? transaction = null)
        {
            await ChangeMessageStateAsync(_pubName, message, state, transaction).ConfigureAwait(false);
        }

        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
        {
            await ChangeMessageStateAsync(_recName, message, state).ConfigureAwait(false);
        }

        public async Task<MediumMessage> StoreMessageAsync(string name, Message content, object? transaction = null)
        {
            var sql =
                $@"INSERT INTO {_pubName} 
(""Id"",""Version"",""Name"",""Content"",""Retries"",""Added"",""ExpiresAt"",""StatusName"") 
VALUES (:Id,'{_options.Value.Version}',:Name,:Content,:Retries,:Added,:ExpiresAt,:StatusName)";

            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = _serializer.Serialize(content),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            object[] sqlParams =
            {
            new DmParameter("Id", message.DbId),
            new DmParameter("Name", name),
            new DmParameter("Content", message.Content),
            new DmParameter("Retries", message.Retries),
            new DmParameter("Added", message.Added),
            new DmParameter("ExpiresAt", message.ExpiresAt.HasValue ? message.ExpiresAt.Value : DBNull.Value),
            new DmParameter("StatusName", nameof(StatusName.Scheduled))
        };

            if (transaction == null)
            {
                var connection = new DmConnection(_options.Value.ConnectionString);
                await using var _ = connection.ConfigureAwait(false);
                await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
            }
            else
            {
                var dbTrans = transaction as DbTransaction;
                if (dbTrans == null && transaction is IDbContextTransaction dbContextTrans)
                    dbTrans = dbContextTrans.GetDbTransaction();

                var conn = dbTrans?.Connection;
                await conn!.ExecuteNonQueryAsync(sql, dbTrans, sqlParams).ConfigureAwait(false);
            }

            return message;
        }

        public async Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
        {
            object[] sqlParams =
            {
            new DmParameter("Id", _snowflakeId.NextId().ToString()),
            new DmParameter("Name", name),
            new DmParameter("OGroup", group),
            new DmParameter("Content", content),
            new DmParameter("Retries", _capOptions.Value.FailedRetryCount),
            new DmParameter("Added", DateTime.Now),
            new DmParameter("ExpiresAt", DateTime.Now.AddSeconds(_capOptions.Value.FailedMessageExpiredAfter)),
            new DmParameter("StatusName", nameof(StatusName.Failed))
        };

            await StoreReceivedMessage(sqlParams).ConfigureAwait(false);
        }

        public async Task<MediumMessage> StoreReceivedMessageAsync(string name, string group, Message message)
        {
            var mdMessage = new MediumMessage
            {
                DbId = _snowflakeId.NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            object[] sqlParams =
            {
            new DmParameter("Id", mdMessage.DbId),
            new DmParameter("Name", name),
            new DmParameter("OGroup", group),
            new DmParameter("Content", _serializer.Serialize(mdMessage.Origin)),
            new DmParameter("Retries", mdMessage.Retries),
            new DmParameter("Added", mdMessage.Added){ DmSqlType = DmDbType.Date},
            new DmParameter("ExpiresAt", mdMessage.ExpiresAt.HasValue ? mdMessage.ExpiresAt.Value : DBNull.Value){ DmSqlType = DmDbType.Date},
            new DmParameter("StatusName", nameof(StatusName.Scheduled))
        };

            await StoreReceivedMessage(sqlParams).ConfigureAwait(false);

            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            var connection = new DmConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var sql = $@"DELETE FROM {table} 
WHERE ""ExpiresAt"" < :timeout 
  AND (""StatusName"" = '{StatusName.Succeeded}' OR ""StatusName"" = '{StatusName.Failed}') 
  AND ROWNUM <= :batchCount
";
            return await connection.ExecuteNonQueryAsync(sql, null,
                new DmParameter("timeout", timeout) { DmSqlType = DmDbType.Date }, new DmParameter("batchCount", batchCount)).ConfigureAwait(false);
        }

        public Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry(TimeSpan lookbackSeconds)
        {
            return GetMessagesOfNeedRetryAsync(_pubName, lookbackSeconds);
        }

        public Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry(TimeSpan lookbackSeconds)
        {
            return GetMessagesOfNeedRetryAsync(_recName, lookbackSeconds);
        }

        public async Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MediumMessage>, Task> scheduleTask,
            CancellationToken token = default)
        {
            var sql =
                $@"SELECT ""Id"",""Content"",""Retries"",""Added"",""ExpiresAt"" 
FROM {_pubName} 
WHERE ""Version"" = :Version 
  AND ((""ExpiresAt"" < :TwoMinutesLater AND ""StatusName"" = '{StatusName.Delayed}') 
       OR (""ExpiresAt"" < :OneMinutesAgo AND ""StatusName"" = '{StatusName.Queued}'))
";

            object[] sqlParams =
            {
            new DmParameter("Version", _capOptions.Value.Version),
            new DmParameter("TwoMinutesLater", DateTime.Now.AddMinutes(2)),
            new DmParameter("OneMinutesAgo", DateTime.Now.AddMinutes(-1))
        };

            await using var connection = new DmConnection(_options.Value.ConnectionString);
            await connection.OpenAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);
            var messageList = await connection.ExecuteReaderAsync(sql, async reader =>
            {
                var messages = new List<MediumMessage>();
                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    messages.Add(new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = _serializer.Deserialize(reader.GetString(1))!,
                        Retries = reader.GetInt32(2),
                        Added = reader.GetDateTime(3),
                        ExpiresAt = reader.GetDateTime(4)
                    });
                }

                return messages;
            }, transaction, sqlParams).ConfigureAwait(false);

            await scheduleTask(transaction, messageList);

            await transaction.CommitAsync(token);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new DMMonitoringApi(_options, _initializer, _serializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, MediumMessage message, StatusName state,
            object? transaction = null)
        {
            var sql =
                @$"UPDATE {tableName} 
SET ""Content""=:Content, 
    ""Retries""=:Retries, 
    ""ExpiresAt""=:ExpiresAt, 
    ""StatusName""=:StatusName 
WHERE ""Id""=:Id
";

            object[] sqlParams =
            {
            new DmParameter("Content", _serializer.Serialize(message.Origin)),
            new DmParameter("Retries", message.Retries),
            new DmParameter("ExpiresAt",  message.ExpiresAt.HasValue ? message.ExpiresAt.Value : DBNull.Value){ DmSqlType = DmDbType.Date},
            new DmParameter("StatusName", state.ToString()),
            new DmParameter("Id", Int64.Parse(message.DbId)){ DmSqlType = DmDbType.Int64},
        };

            if (transaction is DbTransaction dbTransaction)
            {
                var connection = (DmConnection)dbTransaction.Connection!;
                await connection.ExecuteNonQueryAsync(sql, dbTransaction, sqlParams).ConfigureAwait(false);
            }
            else
            {
                var connection = new DmConnection(_options.Value.ConnectionString);
                await using var _ = connection.ConfigureAwait(false);
                await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
            }
        }

        private async Task StoreReceivedMessage(object[] sqlParams)
        {
            var sql =
                $@"INSERT INTO {_recName}(""Id"",""Version"",""Name"",""Group"",""Content"",""Retries"",""Added"",""ExpiresAt"",""StatusName"") 
VALUES(:Id,'{_capOptions.Value.Version}',:Name,:OGroup,:Content,:Retries,:Added,:ExpiresAt,:StatusName)";

            var connection = new DmConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName, TimeSpan lookbackSeconds)
        {
            var fourMinAgo = DateTime.Now.Subtract(lookbackSeconds);
            var sql =
                $@"SELECT ""Id"",""Content"",""Retries"",""Added"" 
FROM (
    SELECT ""Id"",""Content"",""Retries"",""Added"",ROWNUM 
    FROM {tableName} 
    WHERE ""Retries""<:Retries 
      AND ""Version""=:Version 
      AND ""Added""<:Added 
      AND (""StatusName"" = '{StatusName.Failed}' OR ""StatusName"" = '{StatusName.Scheduled}')
) 
WHERE ROWNUM <= 200
";

            object[] sqlParams =
            {
            new DmParameter("Retries", _capOptions.Value.FailedRetryCount),
            new DmParameter("Version", _capOptions.Value.Version),
            new DmParameter("Added", fourMinAgo)
        };

            var connection = new DmConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var result = await connection.ExecuteReaderAsync(sql, async reader =>
            {
                var messages = new List<MediumMessage>();
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    messages.Add(new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = _serializer.Deserialize(reader.GetString(1))!,
                        Retries = reader.GetInt32(2),
                        Added = reader.GetDateTime(3)
                    });
                }

                return messages;
            }, sqlParams: sqlParams).ConfigureAwait(false);

            return result;
        }
    }
}

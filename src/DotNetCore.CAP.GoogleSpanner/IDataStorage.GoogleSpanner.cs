using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Google.Cloud.Spanner.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    public class GoogleSpannerDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> capOptions;
        private readonly IStorageInitializer initializer;
        private readonly IOptions<GoogleSpannerOptions> options;
        private readonly ISerializer serializer;
        private readonly ISnowflakeId snowflakeId;
        private readonly string pubName;
        private readonly string recName;
        private readonly string lockName;

        public GoogleSpannerDataStorage(
            IOptions<GoogleSpannerOptions> options,
            IOptions<CapOptions> capOptions,
            IStorageInitializer initializer,
            ISerializer serializer,
            ISnowflakeId snowflakeId)
        {
            this.capOptions = capOptions;
            this.initializer = initializer;
            this.options = options;
            this.serializer = serializer;
            this.snowflakeId = snowflakeId;
            pubName = initializer.GetPublishedTableName();
            recName = initializer.GetReceivedTableName();
            lockName = initializer.GetLockTableName();
        }

        public async Task<bool> AcquireLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
        {
            string sql =
                $"UPDATE `{lockName}` SET `Instance`=@Instance, `LastLockTime`=@LastLockTime WHERE `Key`=@Key AND `LastLockTime` < @TTL";

            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var sqlParams = new SpannerParameterCollection()
            {
                { "@Instance",SpannerDbType.String, instance },
                { "@LastLockTime", SpannerDbType.Timestamp, DateTime.UtcNow },
                { "@Key", SpannerDbType.String, key },
                { "@TTL", SpannerDbType.Timestamp , DateTime.UtcNow.Subtract(ttl) }
            };

            var opResult = await connection.CreateDmlCommand(sql, sqlParams).ExecuteNonQueryAsync(token).ConfigureAwait(false);

            return opResult > 0;
        }

        public async Task ReleaseLockAsync(string key, string instance, CancellationToken token = default)
        {
            string sql =
                $"UPDATE `{lockName}` SET `Instance`='',`LastLockTime`=@LastLockTime WHERE `Key`=@Key AND `Instance`=@Instance";

            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);

            var sqlParams = new SpannerParameterCollection()
            {
                { "@Instance",SpannerDbType.String, instance },
                { "@LastLockTime", SpannerDbType.Timestamp, DateTime.UtcNow },
                { "@Key", SpannerDbType.String, key },
            };

            await connection.CreateDmlCommand(sql, sqlParams).ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        public async Task RenewLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
        {
            var sql = $"UPDATE `{lockName}` SET `LastLockTime`=TIMESTAMP_ADD(`LastLockTime`, INTERVAL '{ttl.TotalSeconds}' SECOND) WHERE `Key`=@Key AND `Instance`=@Instance";
            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);

            var sqlParams = new SpannerParameterCollection()
            {
                { "Instance", SpannerDbType.String, instance },
                { "Key", SpannerDbType.String, key }
            };

            await connection.CreateDmlCommand(sql, sqlParams).ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        public async Task ChangePublishStateToDelayedAsync(string[] ids)
        {
            var sql = $"UPDATE `{pubName}` SET `StatusName`='{StatusName.Delayed}' WHERE `Id` IN ({string.Join(',', ids)});";

            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);

            await connection.CreateDmlCommand(sql).ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state, object? transaction = null) =>
               await ChangeMessageStateAsync(pubName, message, state, transaction).ConfigureAwait(false);

        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state) =>
                await ChangeMessageStateAsync(recName, message, state).ConfigureAwait(false);

        public async Task<MediumMessage> StoreMessageAsync(string name, Message content, object? transaction = null)
        {
            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = serializer.Serialize(content),
                Added = DateTime.UtcNow,
                ExpiresAt = null,
                Retries = 0
            };

            var sqlParams = new SpannerParameterCollection()
            {
                { "Id", SpannerDbType.String, message.DbId },
                { "Version", SpannerDbType.String, capOptions.Value.Version },
                { "Name", SpannerDbType.String, name },
                { "Content", SpannerDbType.String, message.Content },
                { "Retries", SpannerDbType.Int64, message.Retries },
                { "Added", SpannerDbType.Timestamp, message.Added },
                { "ExpiresAt", SpannerDbType.Timestamp, DBNull.Value },
                { "StatusName", SpannerDbType.String, nameof(StatusName.Scheduled)}
            };


            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var cmd = connection.CreateInsertCommand(pubName, sqlParams);

            if (transaction != null)
            {
                var dbTrans = transaction as IDbTransaction;
                if (dbTrans == null && transaction is IDbContextTransaction dbContextTrans)
                    dbTrans = dbContextTrans.GetDbTransaction();
                cmd.Transaction = dbTrans as DbTransaction;
            }

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            return message;
        }

        public async Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
        {
            var sqlParams = new SpannerParameterCollection()
            {
                { "Id", SpannerDbType.String, snowflakeId.NextId().ToString() },
                { "Name", SpannerDbType.String, name },
                { "Group", SpannerDbType.String, group },
                { "Content", SpannerDbType.String, content },
                { "Retries", SpannerDbType.Int64, capOptions.Value.FailedRetryCount },
                { "Added", SpannerDbType.Timestamp, DateTime.UtcNow },
                { "ExpiresAt", SpannerDbType.Timestamp, DateTime.UtcNow.AddDays(15) },
                { "StatusName", SpannerDbType.String, nameof(StatusName.Failed)}
            };

            await StoreReceivedMessageAsync(sqlParams).ConfigureAwait(false);
        }

        public async Task<MediumMessage> StoreReceivedMessageAsync(string name, string group, Message message)
        {
            var mdMessage = new MediumMessage
            {
                DbId = snowflakeId.NextId().ToString(),
                Origin = message,
                Added = DateTime.UtcNow,
                ExpiresAt = null,
                Retries = 0
            };

            var sqlParams = new SpannerParameterCollection()
            {
                { "Id", SpannerDbType.String, mdMessage.DbId },
                { "Name", SpannerDbType.String, name },
                { "Group", SpannerDbType.String, group },
                { "Content", SpannerDbType.String, serializer.Serialize(mdMessage.Origin) },
                { "Retries", SpannerDbType.Int64, mdMessage.Retries },
                { "Added", SpannerDbType.Timestamp, mdMessage.Added },
                { "ExpiresAt", SpannerDbType.Timestamp, DBNull.Value },
                { "StatusName", SpannerDbType.String, nameof(StatusName.Scheduled)}
            };

            await StoreReceivedMessageAsync(sqlParams).ConfigureAwait(false);
            return mdMessage;
        }


        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default)
        {
            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);

            var sqlParams = new SpannerParameterCollection()
                {
                    { "timeout", SpannerDbType.Timestamp, timeout },
                    { "batchCount", SpannerDbType.Int64, batchCount },
                };

            var sql = $@"DELETE FROM `{table}` WHERE `Id` IN (SELECT `Id` 
                                                                    FROM `{table}` 
                                                                    WHERE `ExpiresAt` < @timeout 
                                                                    AND (`StatusName` = '{StatusName.Succeeded}' OR `StatusName` = '{StatusName.Failed}')     
                                                                    LIMIT @batchCount)";

            var cmd = connection.CreateDmlCommand(sql, sqlParams);

            return await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(pubName);

        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(recName);

        public async Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MediumMessage>, Task> scheduleTask, CancellationToken token = default)
        {
            var sql =
               $@"SELECT `Id`,`Content`,`Retries`,`Added`,`ExpiresAt` FROM {pubName} WHERE `Version`=@Version 
                AND ((`ExpiresAt`< @TwoMinutesLater AND `StatusName` = '{StatusName.Delayed}') OR (`ExpiresAt`< @OneMinutesAgo AND `StatusName` = '{StatusName.Queued}'))";

            var sqlParams = new SpannerParameterCollection()
            {
                { "Version", SpannerDbType.String, capOptions.Value.Version },
                { "TwoMinutesLater", SpannerDbType.Timestamp, DateTime.UtcNow.AddMinutes(2) },
                { "OneMinutesAgo", SpannerDbType.Timestamp, DateTime.UtcNow.AddMinutes(-1) },
            };

            var messageList = new List<MediumMessage>();

            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var cmd = connection.CreateSelectCommand(sql, sqlParams);

            await connection.OpenAsync(token);

            await using var transaction = await connection.BeginTransactionAsync(token);

            using var reader = await cmd.ExecuteReaderAsync(token)
                                         .ConfigureAwait(false);

            while (await reader.ReadAsync(token)
                                .ConfigureAwait(false))
            {
                var messages = new List<MediumMessage>();
                while (await reader.ReadAsync(token)
                                    .ConfigureAwait(false))
                {
                    messages.Add(new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = serializer.Deserialize(reader.GetString(1))!,
                        Retries = reader.GetInt32(2),
                        Added = reader.GetDateTime(3),
                        ExpiresAt = reader.GetDateTime(4)
                    });
                }
            }

            await scheduleTask(transaction, messageList);

            await transaction.CommitAsync(token);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new GoogleSpannerMonitoringApi(options, initializer, serializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, MediumMessage mdMessage, StatusName state, object? transaction = null)
        {
            var sqlParams = new SpannerParameterCollection()
                {
                    { "Id", SpannerDbType.String, mdMessage.DbId },
                    { "Content", SpannerDbType.String, serializer.Serialize(mdMessage.Origin) },
                    { "Retries", SpannerDbType.Int64, mdMessage.Retries },
                    { "ExpiresAt", SpannerDbType.Timestamp,  mdMessage.ExpiresAt },
                    { "StatusName", SpannerDbType.String, state.ToString("G")}
                };

            if (transaction is DbTransaction dbTransaction)
            {
                var connection = (SpannerConnection)dbTransaction.Connection!;
                var cmd = connection.CreateUpdateCommand(tableName, sqlParams);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            else
            {
                var connection = new SpannerConnection(options.Value.ConnectionString);
                await using var _ = connection.ConfigureAwait(false);
                var cmd = connection.CreateUpdateCommand(tableName, sqlParams);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task StoreReceivedMessageAsync(SpannerParameterCollection sqlParams)
        {
            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            var cmd = connection.CreateInsertCommand(recName, sqlParams);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
        {
            var messages = new List<MediumMessage>();
            var fourMinAgo = DateTime.UtcNow.AddMinutes(-4).ToString("s");
            var sql =
               $"SELECT `Id`,`Content`,`Retries`,`Added` FROM `{tableName}` WHERE `Retries`<@Retries " +
               $"AND `Version`=@Version AND `Added`<@Added AND (`StatusName`='{StatusName.Failed}' OR `StatusName`='{StatusName.Scheduled}') LIMIT 200;";

            var sqlParams = new SpannerParameterCollection
            {
                { "@Retries", SpannerDbType.Int64, capOptions.Value.FailedRetryCount },
                { "@Version", SpannerDbType.String,capOptions.Value.Version },
                { "@Added", SpannerDbType.Timestamp, fourMinAgo }
            };

            var connection = new SpannerConnection(options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);

            var cmd = connection.CreateSelectCommand(sql, sqlParams);

            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync())
            {
                var message = serializer.Deserialize(reader.GetString(1));
                if (message is not null)
                {
                    messages.Add(new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = message,
                        Retries = reader.GetInt32(2),
                        Added = Convert.ToDateTime(reader.GetString(3))
                    });
                }
            }

            return messages;
        }
    }
}

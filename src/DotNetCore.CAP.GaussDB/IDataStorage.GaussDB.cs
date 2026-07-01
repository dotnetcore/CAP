using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using HuaweiCloud.GaussDB;
using HuaweiCloud.GaussDBTypes;

namespace DotNetCore.CAP.GaussDB
{
    /// <summary>
    /// GaussDB 的 CAP 消息存储实现，负责发布/接收消息持久化、状态变更、重试查询和存储锁。
    /// </summary>
    public class GaussDBDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IStorageInitializer _initializer;
        private readonly string _lockName;
        private readonly IOptions<GaussDBOptions> _options;
        private readonly string _pubName;
        private readonly string _recName;
        private readonly ISerializer _serializer;
        private readonly ISnowflakeId _snowflakeId;
        private DbConnectionExtensions.GaussDBCompatibilityMode? _databaseCompatibilityMode;
        /// <summary>
        /// 初始化 GaussDB 消息存储实现。
        /// </summary>
        /// <param name="options">GaussDB 连接配置。</param>
        /// <param name="capOptions">CAP 全局配置。</param>
        /// <param name="initializer">存储初始化器，提供表名和兼容模式。</param>
        /// <param name="serializer">消息序列化器。</param>
        /// <param name="snowflakeId">分布式雪花 ID 生成器。</param>
        public GaussDBDataStorage(
            IOptions<GaussDBOptions> options,
            IOptions<CapOptions> capOptions,
            IStorageInitializer initializer,
            ISerializer serializer,
            ISnowflakeId snowflakeId)
        {
            _capOptions = capOptions;
            _initializer = initializer;
            _options = options;
            _serializer = serializer;
            _snowflakeId = snowflakeId;
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
            _lockName = initializer.GetLockTableName();

            if (initializer is GaussDBStorageInitializer gaussDBStorageInitializer)
            {
                _databaseCompatibilityMode = (DbConnectionExtensions.GaussDBCompatibilityMode)gaussDBStorageInitializer.DBCompatibilityMode;
            }
        }
        /// <summary>
        /// M_Compatibility 模式下是否支持锁行
        /// </summary>
        private bool SupportSkipLocked_IN_M_Compatibility_Mode => false;

        /// <summary>
        /// 尝试获取指定 key 的存储锁；只有锁已过期时才会更新成功。
        /// </summary>
        public async Task<bool> AcquireLockAsync(string key, TimeSpan ttl, string instance,
            CancellationToken token = default)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"UPDATE {_lockName} SET `Instance`=@Instance,`LastLockTime`=@LastLockTime WHERE `Key`=@Key AND `LastLockTime` < @TTL;" :
                $"UPDATE {_lockName} SET \"Instance\"=@Instance,\"LastLockTime\"=@LastLockTime WHERE \"Key\"=@Key AND \"LastLockTime\" < @TTL;";
            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
                new GaussDBParameter("@Instance", instance),
                new GaussDBParameter("@LastLockTime", DateTime.Now),
                new GaussDBParameter("@Key", key),
                new GaussDBParameter("@TTL", DateTime.Now.Subtract(ttl))
            };
            var opResult = await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
            return opResult > 0;
        }

        /// <summary>
        /// 释放当前实例持有的存储锁，避免误清理其它实例的锁。
        /// </summary>
        public async Task ReleaseLockAsync(string key, string instance, CancellationToken token = default)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);
            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"UPDATE {_lockName} SET `Instance`='',`LastLockTime`=@LastLockTime WHERE `Key`=@Key AND `Instance`=@Instance;" :
                $"UPDATE {_lockName} SET \"Instance\"='',\"LastLockTime\"=@LastLockTime WHERE \"Key\"=@Key AND \"Instance\"=@Instance;";
            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
                new GaussDBParameter("@Instance", instance),
                new GaussDBParameter("@LastLockTime", DateTime.MinValue),
                new GaussDBParameter("@Key", key)
            };
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        /// <summary>
        /// 续租当前实例持有的存储锁。
        /// </summary>
        public async Task RenewLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"UPDATE {_lockName} SET `LastLockTime`= date_add(`LastLockTime`, interval {ttl.TotalSeconds} second) WHERE `Key`=@Key AND `Instance`=@Instance;" :
                $"UPDATE {_lockName} SET \"LastLockTime\"=\"LastLockTime\"+interval '{ttl.TotalSeconds}' second WHERE \"Key\"=@Key AND \"Instance\"=@Instance;";
            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
                new GaussDBParameter("@Instance", instance),
                new GaussDBParameter("@Key", key)
            };
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        /// <summary>
        /// 将指定发布消息标记为延迟状态，等待调度器后续投递。
        /// </summary>
        public async Task ChangePublishStateToDelayedAsync(string[] ids)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"UPDATE {_pubName} SET `StatusName`='{StatusName.Delayed}' WHERE `Id` IN ({string.Join(',', ids)});" :
                $"UPDATE {_pubName} SET \"StatusName\"='{StatusName.Delayed}' WHERE \"Id\" IN ({string.Join(',', ids)});";
            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state, object? transaction = null)
        {
            await ChangeMessageStateAsync(_pubName, message, state, transaction).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
        {
            await ChangeMessageStateAsync(_recName, message, state).ConfigureAwait(false);
        }

        /// <summary>
        /// 保存发布消息；如果传入事务，则消息写入与业务事务保持一致。
        /// </summary>
        public async Task<MediumMessage> StoreMessageAsync(string name, Message content, object? transaction = null)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"INSERT INTO {_pubName}(`Id`,`Version`,`Name`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`) " +
                $"VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);" :

                $"INSERT INTO {_pubName} (\"Id\",\"Version\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\") " +
                $"VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

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
                new GaussDBParameter("@Id", long.Parse(message.DbId)),
                new GaussDBParameter("@Name", name),
                new GaussDBParameter("@Content", message.Content),
                new GaussDBParameter("@Retries", message.Retries),
                new GaussDBParameter("@Added", message.Added),
                new GaussDBParameter("@ExpiresAt", GaussDBDbType.Timestamp)
                    { Value = message.ExpiresAt.HasValue ? message.ExpiresAt.Value : DBNull.Value },
                new GaussDBParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            if (transaction == null)
            {
                var connection = _options.Value.CreateConnection();
                await using var _ = connection.ConfigureAwait(false);
                await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
            }
            else
            {
                var dbTrans = transaction as DbTransaction;
                if (dbTrans == null && transaction is IDbContextTransaction dbContextTrans)
                    dbTrans = dbContextTrans.GetDbTransaction();

                var conn = dbTrans?.Connection!;
                await conn.ExecuteNonQueryAsync(sql, dbTrans, sqlParams).ConfigureAwait(false);
            }

            return message;
        }

        /// <summary>
        /// 保存消费侧异常消息，便于监控和后续失败重试。
        /// </summary>
        public async Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
        {
            object[] sqlParams =
            {
                new GaussDBParameter("@Id", _snowflakeId.NextId()),
                new GaussDBParameter("@Name", name),
                new GaussDBParameter("@Group", group),
                new GaussDBParameter("@Content", content),
                new GaussDBParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new GaussDBParameter("@Added", DateTime.Now),
                new GaussDBParameter("@ExpiresAt", DateTime.Now.AddSeconds(_capOptions.Value.FailedMessageExpiredAfter)),
                new GaussDBParameter("@StatusName", nameof(StatusName.Failed))
            };

            await StoreReceivedMessage(sqlParams).ConfigureAwait(false);
        }

        /// <summary>
        /// 保存接收消息，初始状态为 Scheduled。
        /// </summary>
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
                new GaussDBParameter("@Id", long.Parse(mdMessage.DbId)),
                new GaussDBParameter("@Name", name),
                new GaussDBParameter("@Group", group),
                new GaussDBParameter("@Content", _serializer.Serialize(mdMessage.Origin)),
                new GaussDBParameter("@Retries", mdMessage.Retries),
                new GaussDBParameter("@Added", mdMessage.Added),
                new GaussDBParameter("@ExpiresAt", GaussDBDbType.Timestamp)
                    { Value = mdMessage.ExpiresAt.HasValue ? mdMessage.ExpiresAt.Value : DBNull.Value },
                new GaussDBParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            await StoreReceivedMessage(sqlParams).ConfigureAwait(false);

            return mdMessage;
        }

        /// <summary>
        /// 批量删除已过期且已终结的消息，避免一次清理过多记录。
        /// </summary>
        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);

            return await connection.ExecuteNonQueryAsync(
                 mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                 $@"DELETE P FROM {table} AS P
               JOIN (
                   SELECT Id
                   FROM {table}
                   WHERE ExpiresAt < @timeout
                   AND StatusName IN ('{StatusName.Succeeded}', '{StatusName.Failed}')
                   ORDER BY Id
                   LIMIT @batchCount
                   {(SupportSkipLocked_IN_M_Compatibility_Mode ? "FOR UPDATE SKIP LOCKED" : "FOR UPDATE")}
               ) AS T ON P.Id = T.Id;"
                   :
                $@"DELETE FROM {table}
               WHERE ""Id"" IN (
                   SELECT ""Id""
                   FROM {table}
                   WHERE ""ExpiresAt"" < @timeout
                   AND ""StatusName"" IN ('{StatusName.Succeeded}','{StatusName.Failed}')
                   LIMIT @batchCount
               )",
                 null,
                 new GaussDBParameter("@timeout", timeout),
                 new GaussDBParameter("@batchCount", batchCount)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry(TimeSpan lookbackSeconds)
        {
            return await GetMessagesOfNeedRetryAsync(_pubName, lookbackSeconds).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry(TimeSpan lookbackSeconds)
        {
            return await GetMessagesOfNeedRetryAsync(_recName, lookbackSeconds).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> DeleteReceivedMessageAsync(long id)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"DELETE FROM {_recName} WHERE Id={id};" :
                $@"DELETE FROM {_recName} WHERE ""Id""={id}";

            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            var result = await connection.ExecuteNonQueryAsync(sql);
            return result;
        }

        /// <inheritdoc />
        public async Task<int> DeletePublishedMessageAsync(long id)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"DELETE FROM {_pubName} WHERE Id={id};" :
                $@"DELETE FROM {_pubName} WHERE ""Id""={id}";

            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            var result = await connection.ExecuteNonQueryAsync(sql);
            return result;
        }

        /// <summary>
        /// 在事务中拉取待调度的延迟/排队消息，并交给调度器处理后提交事务。
        /// </summary>
        public async Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MediumMessage>, Task> scheduleTask,
            CancellationToken token = default)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);
            var lockSql = SupportSkipLocked_IN_M_Compatibility_Mode ? "FOR UPDATE SKIP LOCKED" : "FOR UPDATE";

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                ($"SELECT `Id`,`Content`,`Retries`,`Added`,`ExpiresAt` FROM {_pubName} WHERE `Version`=@Version " +
                $"AND ((`ExpiresAt`< @TwoMinutesLater AND `StatusName` = '{StatusName.Delayed}') OR (`ExpiresAt`< @OneMinutesAgo AND `StatusName` = '{StatusName.Queued}')) LIMIT @BatchSize {lockSql};")
                :
                ($"SELECT \"Id\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\" FROM {_pubName} WHERE \"Version\"=@Version " +
                $"AND ((\"ExpiresAt\"< @TwoMinutesLater AND \"StatusName\" = '{StatusName.Delayed}') OR (\"ExpiresAt\"< @OneMinutesAgo AND \"StatusName\" = '{StatusName.Queued}')) FOR UPDATE SKIP LOCKED LIMIT @BatchSize;");

            var sqlParams = new object[]
            {
                new GaussDBParameter("@Version", _capOptions.Value.Version),
                new GaussDBParameter("@TwoMinutesLater", DateTime.Now.AddMinutes(2)),
                new GaussDBParameter("@OneMinutesAgo", QueuedMessageFetchTime()),
                new GaussDBParameter("@BatchSize", _capOptions.Value.SchedulerBatchSize)
            };

            await using var connection = _options.Value.CreateConnection();
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

        /// <inheritdoc />
        public IMonitoringApi GetMonitoringApi()
        {
            return new GaussDBMonitoringApi(_options, _initializer, _serializer);
        }

        /// <summary>
        /// 获取排队消息的拉取时间窗口起点，默认为当前时间前 1 分钟。
        /// </summary>
        /// <returns>排队消息的拉取截止时间。</returns>
        protected virtual DateTime QueuedMessageFetchTime()
        {
            return DateTime.Now.AddMinutes(-1);
        }

        private async Task ChangeMessageStateAsync(string tableName, MediumMessage message, StatusName state, object? transaction = null)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"UPDATE {tableName} SET `Content`=@Content,`Retries`=@Retries,`ExpiresAt`=@ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;" :
                $"UPDATE {tableName} SET \"Content\"=@Content,\"Retries\"=@Retries,\"ExpiresAt\"=@ExpiresAt,\"StatusName\"=@StatusName WHERE \"Id\"=@Id";

            object[] sqlParams =
            {
                new GaussDBParameter("@Id", long.Parse(message.DbId)),
                new GaussDBParameter("@Content", _serializer.Serialize(message.Origin)),
                new GaussDBParameter("@Retries", message.Retries),
                new GaussDBParameter("@ExpiresAt", GaussDBDbType.Timestamp)
                    { Value = message.ExpiresAt.HasValue ? message.ExpiresAt.Value : DBNull.Value },
                new GaussDBParameter("@StatusName", state.ToString("G"))
            };

            if (transaction is DbTransaction dbTransaction)
            {
                var connection = (GaussDBConnection)dbTransaction.Connection!;
                await connection.ExecuteNonQueryAsync(sql, dbTransaction, sqlParams).ConfigureAwait(false);
            }
            else
            {
                await using var connection = _options.Value.CreateConnection();
                await using var _ = connection.ConfigureAwait(false);
                await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
            }
        }

        private async Task StoreReceivedMessage(object[] sqlParams)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $@"INSERT INTO {_recName}(`Id`,`Version`,`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`) " +
                $" VALUES(@Id,'{_options.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);"
                :
                $"INSERT INTO {_recName}(\"Id\",\"Version\",\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(@Id,'{_capOptions.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING \"Id\";";

            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        /// <summary>
        /// 查询达到重试时间窗口且尚未超过最大重试次数的消息。
        /// </summary>
        private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName, TimeSpan lookbackSeconds)
        {
            var fourMinAgo = DateTime.Now.Subtract(lookbackSeconds);
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                $"SELECT `Id`,`Content`,`Retries`,`Added` FROM {tableName} WHERE `Retries`<@Retries " +
                $"AND `Version`=@Version AND `Added` < @Added AND `StatusName` IN ('{StatusName.Failed}','{StatusName.Scheduled}') LIMIT 200;"
                :
                $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\" FROM {tableName} WHERE \"Retries\"<@Retries " +
                $"AND \"Version\"=@Version AND \"Added\"<@Added AND \"StatusName\" IN ('{StatusName.Failed}','{StatusName.Scheduled}') LIMIT 200;";

            object[] sqlParams =
            {
                new GaussDBParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new GaussDBParameter("@Version", _capOptions.Value.Version),
                new GaussDBParameter("@Added", fourMinAgo)
            };

            var connection = _options.Value.CreateConnection();
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

        /// <summary>
        /// 获取数据库的兼容模式
        /// </summary>
        /// <returns></returns>
        private async Task<DbConnectionExtensions.GaussDBCompatibilityMode> TryGetGaussDBCompatibilityModeAsync()
        {
            if (_databaseCompatibilityMode.HasValue) return _databaseCompatibilityMode.Value;
            var connection = _options.Value.CreateConnection();
            try
            {
                _databaseCompatibilityMode = await connection.GetGaussDBCompatibilityModeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                connection?.Dispose();
            }
            return _databaseCompatibilityMode.Value;
        }
    }
}

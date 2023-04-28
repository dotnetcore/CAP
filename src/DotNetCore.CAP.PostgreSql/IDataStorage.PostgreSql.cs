// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IStorageInitializer _initializer;
        private readonly IOptions<PostgreSqlOptions> _options;
        private readonly ISerializer _serializer;
        private readonly string _pubName;
        private readonly string _recName;
        private readonly string _lockName;

        public PostgreSqlDataStorage(
            IOptions<PostgreSqlOptions> options,
            IOptions<CapOptions> capOptions,
            IStorageInitializer initializer,
            ISerializer serializer)
        {
            _capOptions = capOptions;
            _initializer = initializer;
            _options = options;
            _serializer = serializer;
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
            _lockName = initializer.GetLockTableName();
        }

        public async Task<bool> AcquireLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
        {
            string sql =
                $"UPDATE {_lockName} SET \"Instance\"=@Instance,\"LastLockTime\"=@LastLockTime WHERE \"Key\"=@Key AND \"LastLockTime\" < @TTL;";
            var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
                new NpgsqlParameter("@Instance", instance),
                new NpgsqlParameter("@LastLockTime", DateTime.Now),
                new NpgsqlParameter("@Key", key),
                new NpgsqlParameter("@TTL",DateTime.Now.Subtract(ttl))
            };
            var opResult = await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
            return opResult > 0;
        }

        public async Task ReleaseLockAsync(string key, string instance, CancellationToken token = default)
        {
            string sql =
                $"UPDATE {_lockName} SET \"Instance\"='',\"LastLockTime\"=@LastLockTime WHERE \"Key\"=@Key AND \"Instance\"=@Instance;";
            var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
                new NpgsqlParameter("@Instance", instance),
                new NpgsqlParameter("@LastLockTime", DateTime.MinValue),
                new NpgsqlParameter("@Key", key)
            };
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        public async Task RenewLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
        {
            var sql = $"UPDATE {_lockName} SET \"LastLockTime\"=\"LastLockTime\"+interval '{ttl.TotalSeconds}' second WHERE \"Key\"=@Key AND \"Instance\"=@Instance;";
            var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            object[] sqlParams =
            {
                new NpgsqlParameter("@Instance", instance),
                new NpgsqlParameter("@Key", key)
            };
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        public async Task ChangePublishStateToDelayedAsync(string[] ids)
        {
            var sql = $"UPDATE {_pubName} SET \"StatusName\"='{StatusName.Delayed}' WHERE \"Id\" IN ({string.Join(',', ids)});";
            var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }

        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state, object? transaction = null) =>
            await ChangeMessageStateAsync(_pubName, message, state, transaction).ConfigureAwait(false);

        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_recName, message, state).ConfigureAwait(false);

        public async Task<MediumMessage> StoreMessageAsync(string name, Message content, object? transaction = null)
        {
            var sql =
                $"INSERT INTO {_pubName} (\"Id\",\"Version\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
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
                new NpgsqlParameter("@Id", long.Parse(message.DbId)),
                new NpgsqlParameter("@Name", name),
                new NpgsqlParameter("@Content", message.Content),
                new NpgsqlParameter("@Retries", message.Retries),
                new NpgsqlParameter("@Added", message.Added),
                new NpgsqlParameter("@ExpiresAt", message.ExpiresAt.HasValue ? message.ExpiresAt.Value : DBNull.Value),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            if (transaction == null)
            {
                var connection = new NpgsqlConnection(_options.Value.ConnectionString);
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

        public async Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
        {
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", SnowflakeId.Default().NextId()),
                new NpgsqlParameter("@Name", name),
                new NpgsqlParameter("@Group", group),
                new NpgsqlParameter("@Content", content),
                new NpgsqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new NpgsqlParameter("@Added", DateTime.Now),
                new NpgsqlParameter("@ExpiresAt", DateTime.Now.AddSeconds(_capOptions.Value.FailedMessageExpiredAfter)),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Failed))
            };

            await StoreReceivedMessage(sqlParams).ConfigureAwait(false);
        }

        public async Task<MediumMessage> StoreReceivedMessageAsync(string name, string group, Message message)
        {
            var mdMessage = new MediumMessage
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", long.Parse(mdMessage.DbId)),
                new NpgsqlParameter("@Name", name),
                new NpgsqlParameter("@Group", group),
                new NpgsqlParameter("@Content", _serializer.Serialize(mdMessage.Origin)),
                new NpgsqlParameter("@Retries", mdMessage.Retries),
                new NpgsqlParameter("@Added", mdMessage.Added),
                new NpgsqlParameter("@ExpiresAt", mdMessage.ExpiresAt.HasValue ? mdMessage.ExpiresAt.Value : DBNull.Value),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            await StoreReceivedMessage(sqlParams).ConfigureAwait(false);

            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            return await connection.ExecuteNonQueryAsync(
                $"DELETE FROM {table} WHERE \"Id\" IN (SELECT \"Id\" FROM {table} WHERE \"ExpiresAt\" < @timeout AND (\"StatusName\"='{StatusName.Succeeded}' OR \"StatusName\"='{StatusName.Failed}') LIMIT @batchCount);", null,
                new NpgsqlParameter("@timeout", timeout), new NpgsqlParameter("@batchCount", batchCount)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_pubName).ConfigureAwait(false);

        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_recName).ConfigureAwait(false);

        public async Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MediumMessage>, Task> scheduleTask, CancellationToken token = default)
        {
            var sql =
                $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\" FROM {_pubName} WHERE \"Version\"=@Version " +
                $"AND ((\"ExpiresAt\"< @TwoMinutesLater AND \"StatusName\" = '{StatusName.Delayed}') OR (\"ExpiresAt\"< @OneMinutesAgo AND \"StatusName\" = '{StatusName.Queued}')) FOR UPDATE SKIP LOCKED;";

            object[] sqlParams =
            {
                new NpgsqlParameter("@Version", _capOptions.Value.Version),
                new NpgsqlParameter("@TwoMinutesLater", DateTime.Now.AddMinutes(2)),
                new NpgsqlParameter("@OneMinutesAgo", DateTime.Now.AddMinutes(-1)),
            };

            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
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
            return new PostgreSqlMonitoringApi(_options, _initializer, _serializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, MediumMessage message, StatusName state, object? transaction = null)
        {
            var sql =
                $"UPDATE {tableName} SET \"Content\"=@Content,\"Retries\"=@Retries,\"ExpiresAt\"=@ExpiresAt,\"StatusName\"=@StatusName WHERE \"Id\"=@Id";

            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", long.Parse(message.DbId)),
                new NpgsqlParameter("@Content", _serializer.Serialize(message.Origin)),
                new NpgsqlParameter("@Retries", message.Retries),
                new NpgsqlParameter("@ExpiresAt", message.ExpiresAt),
                new NpgsqlParameter("@StatusName", state.ToString("G"))
            };

            if (transaction is DbTransaction dbTransaction)
            {
                var connection = (NpgsqlConnection)dbTransaction.Connection!;
                await connection.ExecuteNonQueryAsync(sql, dbTransaction, sqlParams).ConfigureAwait(false);
            }
            else
            {
                await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
                await using var _ = connection.ConfigureAwait(false);
                await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
            }
        }

        private async Task StoreReceivedMessage(object[] sqlParams)
        {
            var sql =
                $"INSERT INTO {_recName}(\"Id\",\"Version\",\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(@Id,'{_capOptions.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING \"Id\";";

            var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }

        private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4);
            var sql =
                $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\" FROM {tableName} WHERE \"Retries\"<@Retries " +
                $"AND \"Version\"=@Version AND \"Added\"<@Added AND (\"StatusName\"='{StatusName.Failed}' OR \"StatusName\"='{StatusName.Scheduled}') LIMIT 200;";

            object[] sqlParams =
            {
                new NpgsqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new NpgsqlParameter("@Version", _capOptions.Value.Version),
                new NpgsqlParameter("@Added", fourMinAgo)
            };

            var connection = new NpgsqlConnection(_options.Value.ConnectionString);
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
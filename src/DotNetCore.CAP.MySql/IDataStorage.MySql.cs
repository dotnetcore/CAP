// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
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
using MySqlConnector;

namespace DotNetCore.CAP.MySql;

public class MySqlDataStorage : IDataStorage
{
    private readonly IOptions<CapOptions> _capOptions;
    private readonly IStorageInitializer _initializer;
    private readonly IOptions<MySqlOptions> _options;
    private readonly string _pubName;
    private readonly string _recName;
    private readonly ISerializer _serializer;
    private readonly string _lockName;

    public MySqlDataStorage(
        IOptions<MySqlOptions> options,
        IOptions<CapOptions> capOptions,
        IStorageInitializer initializer,
        ISerializer serializer)
    {
        _options = options;
        _capOptions = capOptions;
        _initializer = initializer;
        _serializer = serializer;
        _pubName = initializer.GetPublishedTableName();
        _recName = initializer.GetReceivedTableName();
        _lockName = initializer.GetLockTableName();
    }

    bool SupportSkipLocked => ((MySqlStorageInitializer)_initializer).IsSupportSkipLocked();

    public async Task<bool> AcquireLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
    {
        string sql =
            $"UPDATE `{_lockName}` SET `Instance`=@Instance,`LastLockTime`=@LastLockTime WHERE `Key`=@Key AND `LastLockTime` < @TTL;";
        var connection = new MySqlConnection(_options.Value.ConnectionString);
        await using var _ = connection.ConfigureAwait(false);
        object[] sqlParams =
        {
            new MySqlParameter("@Instance", instance),
            new MySqlParameter("@LastLockTime", DateTime.Now),
            new MySqlParameter("@Key", key),
            new MySqlParameter("@TTL",DateTime.Now.Subtract(ttl))
        };
        var opResult = await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        return opResult > 0;
    }

    public async Task ReleaseLockAsync(string key, string instance, CancellationToken token = default)
    {
        string sql =
            $"UPDATE `{_lockName}` SET `Instance`='',`LastLockTime`=@LastLockTime WHERE `Key`=@Key AND `Instance`=@Instance;";
        var connection = new MySqlConnection(_options.Value.ConnectionString);
        await using var _ = connection.ConfigureAwait(false);
        object[] sqlParams =
        {
            new MySqlParameter("@Instance", instance),
            new MySqlParameter("@LastLockTime", DateTime.MinValue),
            new MySqlParameter("@Key", key)
        };
        await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
    }

    public async Task RenewLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
    {
        var sql = $"UPDATE `{_lockName}` SET `LastLockTime`= date_add(`LastLockTime`, interval {ttl.TotalSeconds} second) WHERE `Key`=@Key AND `Instance`=@Instance;";
        var connection = new MySqlConnection(_options.Value.ConnectionString);
        await using var _ = connection.ConfigureAwait(false);
        object[] sqlParams =
        {
            new MySqlParameter("@Instance", instance),
            new MySqlParameter("@Key", key)
        };
        await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
    }

    public async Task ChangePublishStateToDelayedAsync(string[] ids)
    {
        var sql = $"UPDATE `{_pubName}` SET `StatusName`='{StatusName.Delayed}' WHERE `Id` IN ({string.Join(',', ids)});";
        var connection = new MySqlConnection(_options.Value.ConnectionString);
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
            $"INSERT INTO `{_pubName}`(`Id`,`Version`,`Name`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)" +
            $" VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

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
            new MySqlParameter("@Id", message.DbId),
            new MySqlParameter("@Name", name),
            new MySqlParameter("@Content", message.Content),
            new MySqlParameter("@Retries", message.Retries),
            new MySqlParameter("@Added", message.Added),
            new MySqlParameter("@ExpiresAt", message.ExpiresAt.HasValue ? message.ExpiresAt.Value : DBNull.Value),
            new MySqlParameter("@StatusName", nameof(StatusName.Scheduled))
        };

        if (transaction == null)
        {
            var connection = new MySqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }
        else
        {
            var dbTrans = transaction as DbTransaction;
            if (dbTrans == null && transaction is IDbContextTransaction dbContextTrans)
                dbTrans = dbContextTrans.GetDbTransaction();

            var conn = dbTrans!.Connection!;
            await conn.ExecuteNonQueryAsync(sql, dbTrans, sqlParams).ConfigureAwait(false);
        }

        return message;
    }

    public async Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
    {
        object[] sqlParams =
        {
            new MySqlParameter("@Id", SnowflakeId.Default().NextId().ToString()),
            new MySqlParameter("@Name", name),
            new MySqlParameter("@Group", group),
            new MySqlParameter("@Content", content),
            new MySqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
            new MySqlParameter("@Added", DateTime.Now),
            new MySqlParameter("@ExpiresAt", DateTime.Now.AddSeconds(_capOptions.Value.FailedMessageExpiredAfter)),
            new MySqlParameter("@StatusName", nameof(StatusName.Failed))
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
            new MySqlParameter("@Id", mdMessage.DbId),
            new MySqlParameter("@Name", name),
            new MySqlParameter("@Group", group),
            new MySqlParameter("@Content", _serializer.Serialize(mdMessage.Origin)),
            new MySqlParameter("@Retries", mdMessage.Retries),
            new MySqlParameter("@Added", mdMessage.Added),
            new MySqlParameter("@ExpiresAt", mdMessage.ExpiresAt.HasValue ? mdMessage.ExpiresAt.Value : DBNull.Value),
            new MySqlParameter("@StatusName", nameof(StatusName.Scheduled))
        };

        await StoreReceivedMessage(sqlParams).ConfigureAwait(false);
        return mdMessage;
    }

    public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
        CancellationToken token = default)
    {
        var connection = new MySqlConnection(_options.Value.ConnectionString);
        await using var _ = connection.ConfigureAwait(false);
        return await connection.ExecuteNonQueryAsync(
                $@"DELETE FROM `{table}` WHERE ExpiresAt < @timeout AND (StatusName='{StatusName.Succeeded}' OR StatusName='{StatusName.Failed}') limit @batchCount;", null,
                new MySqlParameter("@timeout", timeout), new MySqlParameter("@batchCount", batchCount))
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
    {
        return await GetMessagesOfNeedRetryAsync(_pubName).ConfigureAwait(false);
    }

    public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
    {
        return await GetMessagesOfNeedRetryAsync(_recName).ConfigureAwait(false);
    }

    public async Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MediumMessage>, Task> scheduleTask,
        CancellationToken token = default)
    {
        var lockSql = SupportSkipLocked ? "FOR UPDATE SKIP LOCKED" : "FOR UPDATE";

        var sql =
            $"SELECT `Id`,`Content`,`Retries`,`Added`,`ExpiresAt` FROM `{_pubName}` WHERE `Version`=@Version " +
            $"AND ((`ExpiresAt`< @TwoMinutesLater AND `StatusName` = '{StatusName.Delayed}') OR (`ExpiresAt`< @OneMinutesAgo AND `StatusName` = '{StatusName.Queued}')) {lockSql};";

        object[] sqlParams =
        {
            new MySqlParameter("@Version", _capOptions.Value.Version),
            new MySqlParameter("@TwoMinutesLater", DateTime.Now.AddMinutes(2)),
            new MySqlParameter("@OneMinutesAgo", DateTime.Now.AddMinutes(-1)),
        };

        await using var connection = new MySqlConnection(_options.Value.ConnectionString);

        await connection.OpenAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
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
        return new MySqlMonitoringApi(_options, _initializer, _serializer);
    }

    private async Task ChangeMessageStateAsync(string tableName, MediumMessage message, StatusName state, object? transaction = null)
    {
        var sql =
            $"UPDATE `{tableName}` SET `Content`=@Content,`Retries`=@Retries,`ExpiresAt`=@ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";

        object[] sqlParams =
        {
            new MySqlParameter("@Id", message.DbId),
            new MySqlParameter("@Content", _serializer.Serialize(message.Origin)),
            new MySqlParameter("@Retries", message.Retries),
            new MySqlParameter("@ExpiresAt", message.ExpiresAt),
            new MySqlParameter("@StatusName", state.ToString("G"))
        };

        if (transaction is DbTransaction dbTransaction)
        {
            var connection = (MySqlConnection)dbTransaction.Connection!;
            await connection.ExecuteNonQueryAsync(sql, dbTransaction, sqlParams).ConfigureAwait(false);
        }
        else
        {
            await using var connection = new MySqlConnection(_options.Value.ConnectionString);
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }
    }

    private async Task StoreReceivedMessage(object[] sqlParams)
    {
        var sql =
            $@"INSERT INTO `{_recName}`(`Id`,`Version`,`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`) " +
            $"VALUES(@Id,'{_options.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

        var connection = new MySqlConnection(_options.Value.ConnectionString);
        await using var _ = connection.ConfigureAwait(false);
        await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
    }

    private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
    {
        var fourMinAgo = DateTime.Now.AddMinutes(-4);
        var sql =
            $"SELECT `Id`,`Content`,`Retries`,`Added` FROM `{tableName}` WHERE `Retries`<@Retries " +
            $"AND `Version`=@Version AND `Added`<@Added AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";

        object[] sqlParams =
        {
            new MySqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
            new MySqlParameter("@Version", _capOptions.Value.Version),
            new MySqlParameter("@Added", fourMinAgo)
        };

        var connection = new MySqlConnection(_options.Value.ConnectionString);
        await using var _ = connection.ConfigureAwait(false);
        var result = await connection.ExecuteReaderAsync(sql, async reader =>
        {
            var messages = new List<MediumMessage>();
            while (await reader.ReadAsync().ConfigureAwait(false))
                messages.Add(new MediumMessage
                {
                    DbId = reader.GetInt64(0).ToString(),
                    Origin = _serializer.Deserialize(reader.GetString(1))!,
                    Retries = reader.GetInt32(2),
                    Added = reader.GetDateTime(3)
                });

            return messages;
        }, sqlParams: sqlParams).ConfigureAwait(false);

        return result;
    }
}
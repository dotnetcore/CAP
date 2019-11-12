// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
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
        private readonly IOptions<PostgreSqlOptions> _options;

        public PostgreSqlDataStorage(
            IOptions<PostgreSqlOptions> options,
            IOptions<CapOptions> capOptions)
        {
            _capOptions = capOptions;
            _options = options;
        }

        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state)
        {
            var sql =
                $"UPDATE \"{_options.Value.Schema}\".\"published\" SET \"Retries\"=@Retries,\"ExpiresAt\"=@ExpiresAt,\"StatusName\"=@StatusName WHERE \"Id\"=@Id";
            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await connection.ExecuteAsync(sql, new
            {
                Id = message.DbId,
                message.Retries,
                message.ExpiresAt,
                StatusName = state.ToString("G")
            });
        }

        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
        {
            var sql =
                $"UPDATE \"{_options.Value.Schema}\".\"received\" SET \"Retries\"=@Retries,\"ExpiresAt\"=@ExpiresAt,\"StatusName\"=@StatusName WHERE \"Id\"=@Id";
            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await connection.ExecuteAsync(sql, new
            {
                Id = message.DbId,
                message.Retries,
                message.ExpiresAt,
                StatusName = state.ToString("G")
            });
        }

        public async Task<MediumMessage> StoreMessageAsync(string name, Message content, object dbTransaction = null,
            CancellationToken cancellationToken = default)
        {
            var sql =
                $"INSERT INTO \"{_options.Value.Schema}\".\"published\" (\"Id\",\"Version\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = StringSerializer.Serialize(content),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var po = new
            {
                Id = message.DbId,
                Name = name,
                message.Content,
                message.Retries,
                message.Added,
                message.ExpiresAt,
                StatusName = StatusName.Scheduled
            };

            if (dbTransaction == null)
            {
                await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
                await connection.ExecuteAsync(sql, po);
            }
            else
            {
                var dbTrans = dbTransaction as IDbTransaction;
                if (dbTrans == null && dbTransaction is IDbContextTransaction dbContextTrans)
                    dbTrans = dbContextTrans.GetDbTransaction();

                var conn = dbTrans?.Connection;
                await conn.ExecuteAsync(sql, po, dbTrans);
            }

            return message;
        }

        public async Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
        {
            var sql =
                $"INSERT INTO \"{_options.Value.Schema}\".\"received\"(\"Id\",\"Version\",\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(@Id,'{_capOptions.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING \"Id\";";

            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await connection.ExecuteAsync(sql, new
            {
                Id = SnowflakeId.Default().NextId().ToString(),
                Group = group,
                Name = name,
                Content = content,
                Retries = _capOptions.Value.FailedRetryCount,
                Added = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(15),
                StatusName = nameof(StatusName.Failed)
            });
        }

        public async Task<MediumMessage> StoreReceivedMessageAsync(string name, string group, Message message)
        {
            var sql =
                $"INSERT INTO \"{_options.Value.Schema}\".\"received\"(\"Id\",\"Version\",\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(@Id,'{_capOptions.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING \"Id\";";

            var mdMessage = new MediumMessage
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };
            var content = StringSerializer.Serialize(mdMessage.Origin);
            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            await connection.ExecuteAsync(sql, new
            {
                Id = mdMessage.DbId,
                Group = group,
                Name = name,
                Content = content,
                mdMessage.Retries,
                mdMessage.Added,
                mdMessage.ExpiresAt,
                StatusName = nameof(StatusName.Scheduled)
            });
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);

            return await connection.ExecuteAsync(
                $"DELETE FROM \"{_options.Value.Schema}\".\"{table}\" WHERE \"ExpiresAt\" < @now AND \"Id\" IN (SELECT \"Id\" FROM \"{_options.Value.Schema}\".\"{table}\" LIMIT @count);",
                new {timeout, batchCount});
        }

        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT * FROM \"{_options.Value.Schema}\".\"published\" WHERE \"Retries\"<{_capOptions.Value.FailedRetryCount} AND \"Version\"='{_capOptions.Value.Version}' AND \"Added\"<'{fourMinAgo}' AND (\"StatusName\"='{StatusName.Failed}' OR \"StatusName\"='{StatusName.Scheduled}') LIMIT 200;";

            var result = new List<MediumMessage>();
            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            var reader = await connection.ExecuteReaderAsync(sql);
            while (reader.Read())
            {
                result.Add(new MediumMessage
                {
                    DbId = reader.GetInt64(0).ToString(),
                    Origin = StringSerializer.DeSerialize(reader.GetString(3)),
                    Retries = reader.GetInt32(4),
                    Added = reader.GetDateTime(5)
                });
            }

            return result;
        }

        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT * FROM \"{_options.Value.Schema}\".\"received\" WHERE \"Retries\"<{_capOptions.Value.FailedRetryCount} AND \"Version\"='{_capOptions.Value.Version}' AND \"Added\"<'{fourMinAgo}' AND (\"StatusName\"='{StatusName.Failed}' OR \"StatusName\"='{StatusName.Scheduled}') LIMIT 200;";

            var result = new List<MediumMessage>();

            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            var reader = await connection.ExecuteReaderAsync(sql);
            while (reader.Read())
            {
                result.Add(new MediumMessage
                {
                    DbId = reader.GetInt64(0).ToString(),
                    Origin = StringSerializer.DeSerialize(reader.GetString(3)),
                    Retries = reader.GetInt32(4),
                    Added = reader.GetDateTime(5)
                });
            }

            return result;
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new PostgreSqlMonitoringApi(_options);
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlDataStorage : IDataStorage
    {
        private readonly IOptions<MySqlOptions> _options;
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IStorageInitializer _initializer;
        private readonly string _pubName;
        private readonly string _recName;

        public MySqlDataStorage(
            IOptions<MySqlOptions> options,
            IOptions<CapOptions> capOptions,
            IStorageInitializer initializer)
        {
            _options = options;
            _capOptions = capOptions;
            _initializer = initializer;
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
        }

        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_pubName, message, state);

        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_recName, message, state);

        public MediumMessage StoreMessage(string name, Message content, object dbTransaction = null)
        {
            var sql = $"INSERT INTO `{_pubName}`(`Id`,`Version`,`Name`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)" +
                      $" VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = StringSerializer.Serialize(content),
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
                new MySqlParameter("@ExpiresAt", message.ExpiresAt.HasValue ? (object)message.ExpiresAt.Value : DBNull.Value),
                new MySqlParameter("@StatusName", nameof(StatusName.Scheduled)),
            };

            if (dbTransaction == null)
            {
                using var connection = new MySqlConnection(_options.Value.ConnectionString);
                connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
            }
            else
            {
                var dbTrans = dbTransaction as IDbTransaction;
                if (dbTrans == null && dbTransaction is IDbContextTransaction dbContextTrans)
                {
                    dbTrans = dbContextTrans.GetDbTransaction();
                }

                var conn = dbTrans?.Connection;
                conn.ExecuteNonQuery(sql, dbTrans, sqlParams);
            }

            return message;
        }

        public void StoreReceivedExceptionMessage(string name, string group, string content)
        {
            object[] sqlParams =
            {
                new MySqlParameter("@Id", SnowflakeId.Default().NextId().ToString()),
                new MySqlParameter("@Name", name),
                new MySqlParameter("@Group", group),
                new MySqlParameter("@Content", content),
                new MySqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new MySqlParameter("@Added", DateTime.Now),
                new MySqlParameter("@ExpiresAt", DateTime.Now.AddDays(15)),
                new MySqlParameter("@StatusName", nameof(StatusName.Failed))
            };

            StoreReceivedMessage(sqlParams);
        }

        public MediumMessage StoreReceivedMessage(string name, string group, Message message)
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
                new MySqlParameter("@Content", StringSerializer.Serialize(mdMessage.Origin)),
                new MySqlParameter("@Retries", mdMessage.Retries),
                new MySqlParameter("@Added", mdMessage.Added),
                new MySqlParameter("@ExpiresAt", mdMessage.ExpiresAt.HasValue ? (object) mdMessage.ExpiresAt.Value : DBNull.Value),
                new MySqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            StoreReceivedMessage(sqlParams);
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default)
        {
            await using var connection = new MySqlConnection(_options.Value.ConnectionString);
            return connection.ExecuteNonQuery(
                $@"DELETE FROM `{table}` WHERE ExpiresAt < @timeout limit @batchCount;", null,
                new MySqlParameter("@timeout", timeout), new MySqlParameter("@batchCount", batchCount));
        }

        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql = $"SELECT * FROM `{_pubName}` WHERE `Retries`<{_capOptions.Value.FailedRetryCount} AND `Version`='{_capOptions.Value.Version}' AND `Added`<'{fourMinAgo}' AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";

            return await GetMessagesOfNeedRetryAsync(sql);
        }

        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT * FROM `{_recName}` WHERE `Retries`<{_capOptions.Value.FailedRetryCount} AND `Version`='{_capOptions.Value.Version}' AND `Added`<'{fourMinAgo}' AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";

            return await GetMessagesOfNeedRetryAsync(sql);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new MySqlMonitoringApi(_options, _initializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, MediumMessage message, StatusName state)
        {
            var sql =
                $"UPDATE `{tableName}` SET `Retries` = @Retries,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";

            object[] sqlParams =
            {
                new MySqlParameter("@Id", message.DbId),
                new MySqlParameter("@Retries", message.Retries),
                new MySqlParameter("@ExpiresAt", message.ExpiresAt),
                new MySqlParameter("@StatusName", state.ToString("G"))
            };

            await using var connection = new MySqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
        }

        private void StoreReceivedMessage(object[] sqlParams)
        {
            var sql = $@"INSERT INTO `{_recName}`(`Id`,`Version`,`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`) " +
                      $"VALUES(@Id,'{_options.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using var connection = new MySqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
        }

        private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string sql)
        {
            await using var connection = new MySqlConnection(_options.Value.ConnectionString);
            var result = connection.ExecuteReader(sql, reader =>
            {
                var messages = new List<MediumMessage>();
                while (reader.Read())
                {
                    messages.Add(new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = StringSerializer.DeSerialize(reader.GetString(1)),
                        Retries = reader.GetInt32(2),
                        Added = reader.GetDateTime(3)
                    });
                }

                return messages;
            });

            return result;
        }
    }
}

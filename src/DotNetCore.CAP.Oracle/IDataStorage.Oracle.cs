// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Oracle
{
    public class OracleDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IStorageInitializer _initializer;
        private readonly IOptions<OracleOptions> _options;
        private readonly string _pubName;
        private readonly string _recName;

        public OracleDataStorage(
            IOptions<OracleOptions> options,
            IOptions<CapOptions> capOptions,
            IStorageInitializer initializer)
        {
            _capOptions = capOptions;
            _initializer = initializer;
            _options = options;
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
        }

        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_pubName, message, state);

        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_recName, message, state);

        public MediumMessage StoreMessage(string name, Message content, object dbTransaction = null)
        {
            var sql = $"INSERT INTO {_pubName} (\"Id\",\"Version\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(:Id,'{_options.Value.Version}',:Name,:Content,:Retries,:Added,:ExpiresAt,:StatusName)";

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
                new OracleParameter(":Id", long.Parse(message.DbId)),
                new OracleParameter(":Name", name),
                new OracleParameter(":Content", OracleDbType.Clob)
                {
                    Value = message.Content
                },
                new OracleParameter(":Retries", message.Retries),
                new OracleParameter(":Added", message.Added),
                new OracleParameter(":ExpiresAt", message.ExpiresAt.HasValue ? (object)message.ExpiresAt.Value : DBNull.Value),
                new OracleParameter(":StatusName", nameof(StatusName.Scheduled))
            };


            if (dbTransaction == null)
            {
                using var connection = new OracleConnection(_options.Value.ConnectionString);
                connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
            }
            else
            {
                var dbTrans = dbTransaction as IDbTransaction;
                if (dbTrans == null && dbTransaction is IDbContextTransaction dbContextTrans)
                    dbTrans = dbContextTrans.GetDbTransaction();

                var conn = dbTrans?.Connection;
                conn.ExecuteNonQuery(sql, dbTrans, sqlParams);
            }

            return message;
        }

        public void StoreReceivedExceptionMessage(string name, string group, string content)
        {
            object[] sqlParams =
            {
                new OracleParameter(":Id", SnowflakeId.Default().NextId()),
                new OracleParameter(":Name", name),
                new OracleParameter(":Group", group),
                new OracleParameter(":Content", OracleDbType.Clob)
                {
                    Value = content
                },
                new OracleParameter(":Retries", _capOptions.Value.FailedRetryCount),
                new OracleParameter(":Added", DateTime.Now),
                new OracleParameter(":ExpiresAt", DateTime.Now.AddDays(15)),
                new OracleParameter(":StatusName", nameof(StatusName.Failed))
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
                new OracleParameter(":Id", long.Parse(mdMessage.DbId)),
                new OracleParameter(":Name", name),
                new OracleParameter(":Group1", group),
                new OracleParameter(":Content",OracleDbType.Clob)
                {
                    Value = StringSerializer.Serialize(mdMessage.Origin)
                },
                new OracleParameter(":Retries", mdMessage.Retries),
                new OracleParameter(":Added", mdMessage.Added),
                new OracleParameter(":ExpiresAt", mdMessage.ExpiresAt.HasValue ? (object) mdMessage.ExpiresAt.Value : DBNull.Value),
                new OracleParameter(":StatusName", nameof(StatusName.Scheduled))
            };

            StoreReceivedMessage(sqlParams);
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            using var connection = new OracleConnection(_options.Value.ConnectionString);

            var sql = $"DELETE FROM {table} WHERE \"ExpiresAt\" < :timeout AND \"Id\" IN (SELECT \"Id\" FROM {table} WHERE ROWNUM<= :batchCount)";

            var count = connection.ExecuteNonQuery(
               sql, null,
                new OracleParameter(":timeout", timeout), new OracleParameter(":batchCount", batchCount));

            return await Task.FromResult(count);
        }

        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_pubName);

        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_recName);

        public IMonitoringApi GetMonitoringApi()
        {
            return new PostgreSqlMonitoringApi(_options, _initializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, MediumMessage message, StatusName state)
        {
            var sql = $"UPDATE {tableName} SET \"Retries\"=:Retries,\"ExpiresAt\"=:ExpiresAt,\"StatusName\"=:StatusName WHERE \"Id\"=:Id";

            object[] sqlParams =
            {
                new OracleParameter(":Retries", message.Retries),
                new OracleParameter(":ExpiresAt",message.ExpiresAt.HasValue?(object)message.ExpiresAt:DBNull.Value),
                new OracleParameter(":StatusName", state.ToString("G")),
                new OracleParameter(":Id", long.Parse(message.DbId))
            };

            using var connection = new OracleConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);

            await Task.CompletedTask;
        }

        private void StoreReceivedMessage(object[] sqlParams)
        {
            var sql = $"INSERT INTO {_recName} (\"Id\",\"Version\",\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $" VALUES (:Id,'{_capOptions.Value.Version}',:Name,:Group1,:Content,:Retries,:Added,:ExpiresAt,:StatusName) ";
            using var connection = new OracleConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
        }

        private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql = $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\" FROM {tableName} WHERE \"Retries\"<{_capOptions.Value.FailedRetryCount} " +
                $"AND \"Version\"='{_capOptions.Value.Version}' AND \"Added\"<'{fourMinAgo}' AND (\"StatusName\"='{StatusName.Failed}' OR \"StatusName\"='{StatusName.Scheduled}') AND ROWNUM<= 200";

            using var connection = new OracleConnection(_options.Value.ConnectionString);
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

            return await Task.FromResult(result);
        }
    }
}

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
using Oracle.ManagedDataAccess.Client;

namespace DotNetCore.CAP.Oracle
{
    public class OracleDataStorage : IDataStorage
    {
        private readonly IOptions<OracleOptions> _options;
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IStorageInitializer _initializer;
        private readonly string _pubName;
        private readonly string _recName;

        public OracleDataStorage(
            IOptions<OracleOptions> options,
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
            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = StringSerializer.Serialize(content),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var sql = $"INSERT INTO \"{_pubName}\"(\"Id\",\"Version\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                      $" VALUES(:P_Id,'{_options.Value.Version}',:P_Name,:P_Content,:P_Retries,:P_Added,:P_ExpiresAt,:P_StatusName)";

            object[] sqlParams =
            {
                new OracleParameter(":P_Id", message.DbId),
                new OracleParameter(":P_Name", name),
                new OracleParameter(":P_Content", message.Content),
                new OracleParameter(":P_Retries", message.Retries),
                new OracleParameter(":P_Added", message.Added),
                new OracleParameter(":P_ExpiresAt", message.ExpiresAt.HasValue ? (object)message.ExpiresAt.Value : DBNull.Value),
                new OracleParameter(":P_StatusName", nameof(StatusName.Scheduled)),
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
                new OracleParameter(":P_Id", SnowflakeId.Default().NextId().ToString()),
                new OracleParameter(":P_Name", name),
                new OracleParameter(":P_Group", group),
                new OracleParameter(":P_Content", content),
                new OracleParameter(":P_Retries", _capOptions.Value.FailedRetryCount),
                new OracleParameter(":P_Added", DateTime.Now),
                new OracleParameter(":P_ExpiresAt", DateTime.Now.AddDays(15)),
                new OracleParameter(":P_StatusName", nameof(StatusName.Failed))
            };

            StoreReceivedMessage(sqlParams, DateTime.Now, DateTime.Now.AddDays(15));
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
                new OracleParameter(":P_Id", mdMessage.DbId),
                new OracleParameter(":P_Name", name),
                new OracleParameter(":P_Group", group),
                new OracleParameter(":P_Content", StringSerializer.Serialize(mdMessage.Origin)),
                new OracleParameter(":P_Retries", mdMessage.Retries),
                new OracleParameter(":P_Added", mdMessage.Added),
                new OracleParameter(":P_ExpiresAt", mdMessage.ExpiresAt.HasValue ? (object) mdMessage.ExpiresAt.Value : DBNull.Value),
                new OracleParameter(":P_StatusName", nameof(StatusName.Scheduled))
            };

            StoreReceivedMessage(sqlParams, mdMessage.Added, mdMessage.ExpiresAt);
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default)
        {
            using var connection = new OracleConnection(_options.Value.ConnectionString);
            //var sql = $@"DELETE FROM ""{table}"" WHERE ""ExpiresAt"" < {timeout.ToString().BootstrapDateFunction()} AND ROWNUM <= {batchCount}";
            //return await Task.FromResult(connection.ExecuteNonQuery(sql));
            var sql = $@"DELETE FROM ""{table}"" WHERE ""ExpiresAt"" < :timeout AND ROWNUM <= :batchCount";
            return await Task.FromResult(connection.ExecuteNonQuery(sql, null, new OracleParameter(":timeout", timeout), new OracleParameter(":batchCount", batchCount)));
        }

        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_pubName);

        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_recName);

        public IMonitoringApi GetMonitoringApi()
        {
            return new OracleMonitoringApi(_options, _initializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, MediumMessage message, StatusName state)
        {
            var sql =
                $@"UPDATE ""{tableName}"" SET ""Retries"" = :P_Retries,""ExpiresAt"" = :P_ExpiresAt,""StatusName"" = :P_StatusName WHERE ""Id"" = :P_Id";

            object[] sqlParams =
            {
                new OracleParameter(":P_Retries", message.Retries),
                new OracleParameter(":P_ExpiresAt", message.ExpiresAt),
                new OracleParameter(":P_StatusName", state.ToString("G")),
                new OracleParameter(":P_Id", message.DbId)
            };

            using var connection = new OracleConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
            await Task.CompletedTask;
        }

        private void StoreReceivedMessage(object[] sqlParams, DateTime added, DateTime? expiresAt)
        {
            var sql = $@"INSERT INTO ""{ _recName}"" (""Id"",""Version"",""Name"",""Group"",""Content"",""Retries"",""Added"",""ExpiresAt"",""StatusName"")
                      VALUES(:P_Id,'{_options.Value.Version}',:P_Name,:P_Group,:P_Content,:P_Retries,:P_Added,:P_ExpiresAt,:P_StatusName)";

            using var connection = new OracleConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
        }

        private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4);
            var sql =
                $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\" FROM \"{tableName}\" WHERE \"Retries\"<{_capOptions.Value.FailedRetryCount} " +
                $"AND \"Version\"='{_capOptions.Value.Version}' AND \"Added\"<:P_FourMinAgo AND (\"StatusName\" = '{StatusName.Failed}' OR \"StatusName\" = '{StatusName.Scheduled}') AND ROWNUM <= 200";

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
            }, new OracleParameter(":P_FourMinAgo", fourMinAgo));

            return result;
        }
    }
}

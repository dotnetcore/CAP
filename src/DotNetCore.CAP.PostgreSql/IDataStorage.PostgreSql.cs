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
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public partial class PostgreSqlDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IStorageInitializer _initializer;
        private readonly IOptions<PostgreSqlOptions> _options;
        //private readonly ISerializer _serializer;
        private readonly string _pubName;
        private readonly string _recName;
        private readonly ISerializerRegistry _serializerRegistry;


        public PostgreSqlDataStorage(
            IOptions<PostgreSqlOptions> options,
            IOptions<CapOptions> capOptions,
            IStorageInitializer initializer,
            //ISerializer serializer,
            ISerializerRegistry serializerRegistry
            )
        {
            _capOptions = capOptions;
            _initializer = initializer;
            _options = options;
            //_serializer = serializer;
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
            _serializerRegistry = serializerRegistry;
        }

        public async Task ChangePublishStateAsync(IMediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_pubName, message, state);

        public async Task ChangeReceiveStateAsync(IMediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_recName, message, state);

        public IMediumMessage StoreMessage(string name, ICapMessage content, object dbTransaction = null)
        {
            var sql =
                $"INSERT INTO {_pubName} (\"Id\",\"Version\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            var serializer = _serializerRegistry.GetMessageSerializer(content.GetMessageType());

            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = serializer.Serialize(content),
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
                new NpgsqlParameter("@ExpiresAt", message.ExpiresAt.HasValue ? (object)message.ExpiresAt.Value : DBNull.Value),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            if (dbTransaction == null)
            {
                using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
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
                new NpgsqlParameter("@Id", SnowflakeId.Default().NextId()),
                new NpgsqlParameter("@Name", name),
                new NpgsqlParameter("@Group", group),
                new NpgsqlParameter("@Content", content),
                new NpgsqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new NpgsqlParameter("@Added", DateTime.Now),
                new NpgsqlParameter("@ExpiresAt", DateTime.Now.AddDays(15)),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Failed))
            };

            StoreReceivedMessage(sqlParams);
        }

        public IMediumMessage StoreReceivedMessage(string name, string group, ICapMessage message)
        {
            var mdMessage = new MediumMessage
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var serilizer = _serializerRegistry.GetMessageSerializer(message.GetMessageType());

            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", long.Parse(mdMessage.DbId)),
                new NpgsqlParameter("@Name", name),
                new NpgsqlParameter("@Group", group),
                new NpgsqlParameter("@Content", serilizer.Serialize(mdMessage.Origin)),
                new NpgsqlParameter("@Retries", mdMessage.Retries),
                new NpgsqlParameter("@Added", mdMessage.Added),
                new NpgsqlParameter("@ExpiresAt", mdMessage.ExpiresAt.HasValue ? (object) mdMessage.ExpiresAt.Value : DBNull.Value),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            StoreReceivedMessage(sqlParams);
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            var count = connection.ExecuteNonQuery(
                $"DELETE FROM {table} WHERE \"Id\" IN (SELECT \"Id\" FROM {table} WHERE \"ExpiresAt\" < @timeout LIMIT @batchCount);", null,
                new NpgsqlParameter("@timeout", timeout), new NpgsqlParameter("@batchCount", batchCount));

            return await Task.FromResult(count);
        }

        public async Task<IEnumerable<IMediumMessage>> GetPublishedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_pubName);

        public async Task<IEnumerable<IMediumMessage>> GetReceivedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_recName);

        public IMonitoringApi GetMonitoringApi()
        {
            return new PostgreSqlMonitoringApi(_options, _initializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, IMediumMessage message, StatusName state)
        {
            var sql =
                $"UPDATE {tableName} SET \"Content\"=@Content,\"Retries\"=@Retries,\"ExpiresAt\"=@ExpiresAt,\"StatusName\"=@StatusName WHERE \"Id\"=@Id";

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetOriginType());


            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", long.Parse(message.DbId)),
                new NpgsqlParameter("@Content", serializer.Serialize(message.Origin)),
                new NpgsqlParameter("@Retries", message.Retries),
                new NpgsqlParameter("@ExpiresAt", message.ExpiresAt),
                new NpgsqlParameter("@StatusName", state.ToString("G"))
            };

            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);

            await Task.CompletedTask;
        }

        private void StoreReceivedMessage(object[] sqlParams)
        {
            var sql =
                $"INSERT INTO {_recName}(\"Id\",\"Version\",\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(@Id,'{_capOptions.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING \"Id\";";

            using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
        }

        private async Task<IEnumerable<IMediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\" FROM {tableName} WHERE \"Retries\"<{_capOptions.Value.FailedRetryCount} " +
                $"AND \"Version\"='{_capOptions.Value.Version}' AND \"Added\"<'{fourMinAgo}' AND (\"StatusName\"='{StatusName.Failed}' OR \"StatusName\"='{StatusName.Scheduled}') LIMIT 200;";

            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);

            var serializer = _serializerRegistry.GetMessageSerializer();

            var result = connection.ExecuteReader(sql, reader =>
            {
                var messages = new List<IMediumMessage>();
                while (reader.Read())
                {
                    messages.Add(new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = serializer.Deserialize(reader.GetString(1)),
                        Retries = reader.GetInt32(2),
                        Added = reader.GetDateTime(3)
                    });
                }

                return messages;
            });

            return result;
        }
    }


    public partial class PostgreSqlDataStorage : IDataStorage
    {

        public async Task ChangePublishStateAsync<T>(IMediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_pubName, message, state);

        public async Task ChangeReceiveStateAsync<T>(IMediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_recName, message, state);

        public IMediumMessage StoreMessage<T>(string name, ICapMessage content, object dbTransaction = null)
        {
            var sql =
                $"INSERT INTO {_pubName} (\"Id\",\"Version\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
                $"VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            var type = content.GetMessageType();

            var serializer = _serializerRegistry.GetMessageSerializer(content.GetMessageType());

            var message = new MediumMessage<T>
            {
                DbId = content.GetId(),
                Origin = content,
                Content = serializer.Serialize(content),
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
                new NpgsqlParameter("@ExpiresAt", message.ExpiresAt.HasValue ? (object)message.ExpiresAt.Value : DBNull.Value),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            if (dbTransaction == null)
            {
                using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
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

        public void StoreReceivedExceptionMessage<T>(string name, string group, string content)
        {
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", SnowflakeId.Default().NextId()),
                new NpgsqlParameter("@Name", name),
                new NpgsqlParameter("@Group", group),
                new NpgsqlParameter("@Content", content),
                new NpgsqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new NpgsqlParameter("@Added", DateTime.Now),
                new NpgsqlParameter("@ExpiresAt", DateTime.Now.AddDays(15)),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Failed))
            };

            StoreReceivedMessage(sqlParams);
        }

        public IMediumMessage StoreReceivedMessage<T>(string name, string group, ICapMessage message)
        {
            var mdMessage = new MediumMessage<T>
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = message,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetMessageType());

            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", long.Parse(mdMessage.DbId)),
                new NpgsqlParameter("@Name", name),
                new NpgsqlParameter("@Group", group),
                new NpgsqlParameter("@Content", serializer.Serialize(mdMessage.Origin)),
                new NpgsqlParameter("@Retries", mdMessage.Retries),
                new NpgsqlParameter("@Added", mdMessage.Added),
                new NpgsqlParameter("@ExpiresAt", mdMessage.ExpiresAt.HasValue ? (object) mdMessage.ExpiresAt.Value : DBNull.Value),
                new NpgsqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            StoreReceivedMessage(sqlParams);
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync<T>(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            var count = connection.ExecuteNonQuery(
                $"DELETE FROM {table} WHERE \"Id\" IN (SELECT \"Id\" FROM {table} WHERE \"ExpiresAt\" < @timeout LIMIT @batchCount);", null,
                new NpgsqlParameter("@timeout", timeout), new NpgsqlParameter("@batchCount", batchCount));

            return await Task.FromResult(count);
        }

        public async Task<IEnumerable<IMediumMessage>> GetPublishedMessagesOfNeedRetry<T>() =>
            await GetMessagesOfNeedRetryAsync<T>(_pubName);

        public async Task<IEnumerable<IMediumMessage>> GetReceivedMessagesOfNeedRetry<T>() =>
            await GetMessagesOfNeedRetryAsync<T>(_recName);


        private async Task ChangeMessageStateAsync<T>(string tableName, IMediumMessage message, StatusName state)
        {
            var sql =
                $"UPDATE {tableName} SET \"Content\"=@Content,\"Retries\"=@Retries,\"ExpiresAt\"=@ExpiresAt,\"StatusName\"=@StatusName WHERE \"Id\"=@Id";

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetOriginType());

            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", long.Parse(message.DbId)),
                new NpgsqlParameter("@Content", serializer.Serialize(message.Origin)),
                new NpgsqlParameter("@Retries", message.Retries),
                new NpgsqlParameter("@ExpiresAt", message.ExpiresAt),
                new NpgsqlParameter("@StatusName", state.ToString("G"))
            };

            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);

            await Task.CompletedTask;
        }


        private async Task<IEnumerable<IMediumMessage>> GetMessagesOfNeedRetryAsync<T>(string tableName)
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\" FROM {tableName} WHERE \"Retries\"<{_capOptions.Value.FailedRetryCount} " +
                $"AND \"Version\"='{_capOptions.Value.Version}' AND \"Added\"<'{fourMinAgo}' AND (\"StatusName\"='{StatusName.Failed}' OR \"StatusName\"='{StatusName.Scheduled}') LIMIT 200;";

            await using var connection = new NpgsqlConnection(_options.Value.ConnectionString);

            var serializer = _serializerRegistry.GetMessageSerializer(typeof(T));

            var result = connection.ExecuteReader(sql, reader =>
            {
                var messages = new List<IMediumMessage>();
                while (reader.Read())
                {
                    messages.Add(new MediumMessage<T>
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = serializer.Deserialize(reader.GetString(1)),
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
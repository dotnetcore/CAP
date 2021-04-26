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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer
{
    public partial class SqlServerDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IOptions<SqlServerOptions> _options;
        private readonly IStorageInitializer _initializer;
        //private readonly ISerializer _serializer;
        private readonly string _pubName;
        private readonly string _recName;

        private readonly ISerializerRegistry _serializerRegistry;

        public SqlServerDataStorage(
            IOptions<CapOptions> capOptions,
            IOptions<SqlServerOptions> options,
            IStorageInitializer initializer,
            ISerializerRegistry serializerRegistry
            //ISerializer serializer,
            )
        {
            _options = options;
            _initializer = initializer;
            _capOptions = capOptions;
            //_serializer = serializer;
            _serializerRegistry = serializerRegistry;
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
        }

        public async Task ChangePublishStateAsync(IMediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_pubName, message, state);

        public async Task ChangeReceiveStateAsync(IMediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_recName, message, state);

        public IMediumMessage StoreMessage(string name, ICapMessage content, object dbTransaction = null)
        {
            var sql = $"INSERT INTO {_pubName} ([Id],[Version],[Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName])" +
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
                new SqlParameter("@Id", message.DbId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Content", message.Content),
                new SqlParameter("@Retries", message.Retries),
                new SqlParameter("@Added", message.Added),
                new SqlParameter("@ExpiresAt", message.ExpiresAt.HasValue ? (object)message.ExpiresAt.Value : DBNull.Value),
                new SqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            if (dbTransaction == null)
            {
                using var connection = new SqlConnection(_options.Value.ConnectionString);
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
                new SqlParameter("@Id", SnowflakeId.Default().NextId().ToString()),
                new SqlParameter("@Name", name),
                new SqlParameter("@Group", group),
                new SqlParameter("@Content", content),
                new SqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new SqlParameter("@Added", DateTime.Now),
                new SqlParameter("@ExpiresAt", DateTime.Now.AddDays(15)),
                new SqlParameter("@StatusName", nameof(StatusName.Failed))
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
                new SqlParameter("@Id", mdMessage.DbId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Group", group),
                new SqlParameter("@Content", serilizer.Serialize(mdMessage.Origin)),
                new SqlParameter("@Retries", mdMessage.Retries),
                new SqlParameter("@Added", mdMessage.Added),
                new SqlParameter("@ExpiresAt", mdMessage.ExpiresAt.HasValue ? (object) mdMessage.ExpiresAt.Value : DBNull.Value),
                new SqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            StoreReceivedMessage(sqlParams);
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            using var connection = new SqlConnection(_options.Value.ConnectionString);
            var count = connection.ExecuteNonQuery(
                $"DELETE TOP (@batchCount) FROM {table} WITH (readpast) WHERE ExpiresAt < @timeout;", null,
                new SqlParameter("@timeout", timeout), new SqlParameter("@batchCount", batchCount));

            return await Task.FromResult(count);
        }

        public async Task<IEnumerable<IMediumMessage>> GetPublishedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_pubName);

        public async Task<IEnumerable<IMediumMessage>> GetReceivedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_recName);

        public IMonitoringApi GetMonitoringApi()
        {
            return new SqlServerMonitoringApi(_options, _initializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, IMediumMessage message, StatusName state)
        {
            var sql =
                $"UPDATE {tableName} SET Content=@Content, Retries=@Retries,ExpiresAt=@ExpiresAt,StatusName=@StatusName WHERE Id=@Id";

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetOriginType());

            object[] sqlParams =
            {
                new SqlParameter("@Id", message.DbId),
                new SqlParameter("@Content", serializer.Serialize(message.Origin)),
                new SqlParameter("@Retries", message.Retries),
                new SqlParameter("@ExpiresAt", message.ExpiresAt),
                new SqlParameter("@StatusName", state.ToString("G"))
            };

            using var connection = new SqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);

            await Task.CompletedTask;
        }

        private void StoreReceivedMessage(object[] sqlParams)
        {
            var sql =
                $"INSERT INTO {_recName}([Id],[Version],[Name],[Group],[Content],[Retries],[Added],[ExpiresAt],[StatusName])" +
                $"VALUES(@Id,'{_capOptions.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using var connection = new SqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
        }

        private async Task<IEnumerable<IMediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT TOP (200) Id, Content, Retries, Added FROM {tableName} WITH (readpast) WHERE Retries<{_capOptions.Value.FailedRetryCount} " +
                $"AND Version='{_capOptions.Value.Version}' AND Added<'{fourMinAgo}' AND (StatusName = '{StatusName.Failed}' OR StatusName = '{StatusName.Scheduled}')";

            List<IMediumMessage> result;

            var serializer = _serializerRegistry.GetMessageSerializer();

            using (var connection = new SqlConnection(_options.Value.ConnectionString))
            {
                result = connection.ExecuteReader(sql, reader =>
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
            }

            return await Task.FromResult(result);
        }

        
    }

    public partial class SqlServerDataStorage : IDataStorage
    {
        public async Task ChangePublishStateAsync<T>(IMediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync<T>(_pubName, message, state);

        public async Task ChangeReceiveStateAsync<T>(IMediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync<T>(_recName, message, state);

        public IMediumMessage StoreMessage<T>(string name, ICapMessage content, object dbTransaction = null)
        {
            var sql = $"INSERT INTO {_pubName} ([Id],[Version],[Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName])" +
                      $"VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

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
                new SqlParameter("@Id", message.DbId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Content", message.Content),
                new SqlParameter("@Retries", message.Retries),
                new SqlParameter("@Added", message.Added),
                new SqlParameter("@ExpiresAt", message.ExpiresAt.HasValue ? (object)message.ExpiresAt.Value : DBNull.Value),
                new SqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            if (dbTransaction == null)
            {
                using var connection = new SqlConnection(_options.Value.ConnectionString);
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
                new SqlParameter("@Id", SnowflakeId.Default().NextId().ToString()),
                new SqlParameter("@Name", name),
                new SqlParameter("@Group", group),
                new SqlParameter("@Content", content),
                new SqlParameter("@Retries", _capOptions.Value.FailedRetryCount),
                new SqlParameter("@Added", DateTime.Now),
                new SqlParameter("@ExpiresAt", DateTime.Now.AddDays(15)),
                new SqlParameter("@StatusName", nameof(StatusName.Failed))
            };

            StoreReceivedMessage<T>(sqlParams);
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

            var serilizer = _serializerRegistry.GetMessageSerializer(message.GetMessageType());

            object[] sqlParams =
            {
                new SqlParameter("@Id", mdMessage.DbId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Group", group),
                new SqlParameter("@Content", serilizer.Serialize(mdMessage.Origin)),
                new SqlParameter("@Retries", mdMessage.Retries),
                new SqlParameter("@Added", mdMessage.Added),
                new SqlParameter("@ExpiresAt", mdMessage.ExpiresAt.HasValue ? (object) mdMessage.ExpiresAt.Value : DBNull.Value),
                new SqlParameter("@StatusName", nameof(StatusName.Scheduled))
            };

            StoreReceivedMessage<T>(sqlParams);
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync<T>(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            using var connection = new SqlConnection(_options.Value.ConnectionString);
            var count = connection.ExecuteNonQuery(
                $"DELETE TOP (@batchCount) FROM {table} WITH (readpast) WHERE ExpiresAt < @timeout;", null,
                new SqlParameter("@timeout", timeout), new SqlParameter("@batchCount", batchCount));

            return await Task.FromResult(count);
        }

        public async Task<IEnumerable<IMediumMessage>> GetPublishedMessagesOfNeedRetry<T>() =>
            await GetMessagesOfNeedRetryAsync<T>(_pubName);

        public async Task<IEnumerable<IMediumMessage>> GetReceivedMessagesOfNeedRetry<T>() =>
            await GetMessagesOfNeedRetryAsync<T>(_recName);


        private async Task ChangeMessageStateAsync<T>(string tableName, IMediumMessage message, StatusName state)
        {
            var sql =
                $"UPDATE {tableName} SET Content=@Content, Retries=@Retries,ExpiresAt=@ExpiresAt,StatusName=@StatusName WHERE Id=@Id";

            var serializer = _serializerRegistry.GetMessageSerializer(message.GetOriginType());

            object[] sqlParams =
            {
                new SqlParameter("@Id", message.DbId),
                new SqlParameter("@Content", serializer.Serialize(message.Origin)),
                new SqlParameter("@Retries", message.Retries),
                new SqlParameter("@ExpiresAt", message.ExpiresAt),
                new SqlParameter("@StatusName", state.ToString("G"))
            };

            using var connection = new SqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);

            await Task.CompletedTask;
        }

        private void StoreReceivedMessage<T>(object[] sqlParams)
        {
            var sql =
                $"INSERT INTO {_recName}([Id],[Version],[Name],[Group],[Content],[Retries],[Added],[ExpiresAt],[StatusName])" +
                $"VALUES(@Id,'{_capOptions.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using var connection = new SqlConnection(_options.Value.ConnectionString);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
        }

        private async Task<IEnumerable<IMediumMessage>> GetMessagesOfNeedRetryAsync<T>(string tableName)
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT TOP (200) Id, Content, Retries, Added FROM {tableName} WITH (readpast) WHERE Retries<{_capOptions.Value.FailedRetryCount} " +
                $"AND Version='{_capOptions.Value.Version}' AND Added<'{fourMinAgo}' AND (StatusName = '{StatusName.Failed}' OR StatusName = '{StatusName.Scheduled}')";

            List<IMediumMessage> result;

            var serializer = _serializerRegistry.GetMessageSerializer();

            using (var connection = new SqlConnection(_options.Value.ConnectionString))
            {
                result = connection.ExecuteReader(sql, reader =>
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
            }

            return await Task.FromResult(result);
        }
    }
}
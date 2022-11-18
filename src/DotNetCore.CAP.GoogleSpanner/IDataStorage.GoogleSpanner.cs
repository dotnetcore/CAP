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

namespace DotNetCore.CAP.GoogleSpanner
{
    public class GoogleSpannerDataStorage : IDataStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IStorageInitializer _initializer;
        private readonly IOptions<GoogleSpannerOptions> _options;
        private readonly ISerializer _serializer;
        private readonly string _pubName;
        private readonly string _recName;

        public GoogleSpannerDataStorage(
            IOptions<GoogleSpannerOptions> options,
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
        }

        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_pubName, message, state);

        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state) =>
            await ChangeMessageStateAsync(_recName, message, state);

        public MediumMessage StoreMessage(string name, Message content, object dbTransaction = null)
        {
            var sql =
                $"INSERT INTO {_pubName} (Id,Version,Name,Content,Retries,Added,ExpiresAt,StatusName)" +
                $"VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName)";

            var message = new MediumMessage
            {
                DbId = content.GetId(),
                Origin = content,
                Content = _serializer.Serialize(content),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var sqlParams = new SpannerParameterCollection()
            {
                { "Id", SpannerDbType.String, message.DbId },
                { "Name", SpannerDbType.String, name },
                { "Content", SpannerDbType.String, message.Content },
                { "Retries", SpannerDbType.Int64, message.Retries },
                { "Added", SpannerDbType.Timestamp, message.Added },
                { "ExpiresAt", SpannerDbType.Timestamp, message.ExpiresAt ?? (object)DBNull.Value },
                { "StatusName", SpannerDbType.String, nameof(StatusName.Scheduled)}
            };


            try
            {
                if (dbTransaction == null)
                {
                    using var connection = new SpannerConnection(_options.Value.ConnectionString);
                    var cmd = connection.CreateDmlCommand(sql, sqlParams);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var dbTrans = dbTransaction as IDbTransaction;
                    if (dbTrans == null && dbTransaction is IDbContextTransaction dbContextTrans)
                        dbTrans = dbContextTrans.GetDbTransaction();

                    using var connection = new SpannerConnection(_options.Value.ConnectionString);
                    var cmd = connection.CreateDmlCommand(sql, sqlParams);
                    cmd.Transaction = (DbTransaction)dbTrans;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return message;
        }

        public void StoreReceivedExceptionMessage(string name, string group, string content)
        {
            var sqlParams = new SpannerParameterCollection()
            {
                { "Id", SpannerDbType.String, SnowflakeId.Default().NextId() },
                { "Name", SpannerDbType.String, name },
                { "GroupName", SpannerDbType.String, group },
                { "Content", SpannerDbType.String, content },
                { "Retries", SpannerDbType.Int64, _capOptions.Value.FailedRetryCount },
                { "Added", SpannerDbType.Timestamp, DateTime.Now },
                { "ExpiresAt", SpannerDbType.Timestamp, DateTime.Now.AddDays(15) },
                { "StatusName", SpannerDbType.String, nameof(StatusName.Failed)}
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

            var sqlParams = new SpannerParameterCollection()
            {
                { "Id", SpannerDbType.String, mdMessage.DbId },
                { "Name", SpannerDbType.String, name },
                { "GroupName", SpannerDbType.String, group },
                { "Content", SpannerDbType.String, _serializer.Serialize(mdMessage.Origin) },
                { "Retries", SpannerDbType.Int64, mdMessage.Retries },
                { "Added", SpannerDbType.Timestamp, mdMessage.Added },
                { "ExpiresAt", SpannerDbType.Timestamp,  mdMessage.ExpiresAt?? (object) DBNull.Value },
                { "StatusName", SpannerDbType.String, nameof(StatusName.Scheduled)}
            };

            StoreReceivedMessage(sqlParams);
            return mdMessage;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default)
        {
            long count = 0;
            try
            {
                using var connection = new SpannerConnection(_options.Value.ConnectionString);
                var sqlParams = new SpannerParameterCollection()
                {
                    { "timeout", SpannerDbType.Timestamp, timeout },
                    { "batchCount", SpannerDbType.Int64, batchCount },
                };
                var sql = $"DELETE FROM {table} WHERE Id IN (SELECT Id FROM {table} WHERE ExpiresAt < @timeout LIMIT @batchCount)";
                var cmd = connection.CreateDmlCommand(sql, sqlParams);

                count = await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return await Task.FromResult((int)count);
        }

        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_pubName);

        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry() =>
            await GetMessagesOfNeedRetryAsync(_recName);

        public IMonitoringApi GetMonitoringApi()
        {
            return new GoogleSpannerMonitoringApi(_options, _initializer);
        }

        private async Task ChangeMessageStateAsync(string tableName, MediumMessage mdMessage, StatusName state)
        {
            var sql =
                $"UPDATE {tableName} SET Content=@Content,Retries=@Retries,ExpiresAt=@ExpiresAt,StatusName=@StatusName WHERE Id=@Id";

            try
            {
                var sqlParams = new SpannerParameterCollection()
                {
                    { "Id", SpannerDbType.String, mdMessage.DbId },
                    { "Content", SpannerDbType.String, _serializer.Serialize(mdMessage.Origin) },
                    { "Retries", SpannerDbType.Int64, mdMessage.Retries },
                    { "ExpiresAt", SpannerDbType.Timestamp,  mdMessage.ExpiresAt },
                    { "StatusName", SpannerDbType.String, state.ToString("G")}
                };

                var connection = new SpannerConnection(_options.Value.ConnectionString);
                var cmd = connection.CreateDmlCommand(sql, sqlParams);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await Task.CompletedTask;
        }

        private void StoreReceivedMessage(SpannerParameterCollection sqlParams)
        {
            var sql =
                $"INSERT INTO {_recName}(Id,Version,Name,GroupName,Content,Retries,Added,ExpiresAt,StatusName)" +
                $"VALUES(@Id,'{_capOptions.Value.Version}',@Name,@GroupName,@Content,@Retries,@Added,@ExpiresAt,@StatusName)";

            try
            {
                using var connection = new SpannerConnection(_options.Value.ConnectionString);
                var cmd = connection.CreateDmlCommand(sql, sqlParams);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task<IEnumerable<MediumMessage>> GetMessagesOfNeedRetryAsync(string tableName)
        {
            var messages = new List<MediumMessage>();
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("s");
            var sql =
                $"SELECT Id,Content,Retries,Added FROM {tableName} WHERE Retries<{_capOptions.Value.FailedRetryCount} " +
                $"AND Version='{_capOptions.Value.Version}' AND Added<'{fourMinAgo}' AND (StatusName='{StatusName.Failed}' OR StatusName='{StatusName.Scheduled}') LIMIT 200;";

            try
            {
                using var connection = new SpannerConnection(_options.Value.ConnectionString);
                var cmd = connection.CreateSelectCommand(sql);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    messages.Add(new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = _serializer.Deserialize(reader.GetString(1)),
                        Retries = reader.GetInt32(2),
                        Added = Convert.ToDateTime(reader.GetString(3))
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return messages;
        }
    }
}

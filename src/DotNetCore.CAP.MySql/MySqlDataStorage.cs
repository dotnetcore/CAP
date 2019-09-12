using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Messages;
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

        public MySqlDataStorage(IOptions<MySqlOptions> options, IOptions<CapOptions> capOptions)
        {
            _options = options;
            _capOptions = capOptions;
        }

        public async Task ChangePublishStateAsync(MediumMessage message, StatusName state)
        {
            using (var connection = new MySqlConnection(_options.Value.ConnectionString))
            {
                var sql = $"UPDATE `{_options.Value.TableNamePrefix}.published` SET `Retries` = @Retries,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";

                await connection.ExecuteAsync(sql, new
                {
                    Id = message.DbId,
                    Retries = message.Retries,
                    ExpiresAt = message.ExpiresAt,
                    StatusName = state.ToString("G")
                });
            }
        }

        public async Task ChangeReceiveStateAsync(MediumMessage message, StatusName state)
        {
            using (var connection = new MySqlConnection(_options.Value.ConnectionString))
            {
                var sql = $"UPDATE `{_options.Value.TableNamePrefix}.received` SET `Retries` = @Retries,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";

                await connection.ExecuteAsync(sql, new
                {
                    Id = message.DbId,
                    Retries = message.Retries,
                    ExpiresAt = message.ExpiresAt,
                    StatusName = state.ToString("G")
                });
            }
        }

        public async Task<MediumMessage> StoreMessageAsync(string name, Message content, object dbTransaction = null, CancellationToken cancellationToken = default)
        {
            var sql = $"INSERT INTO `{_options.Value.TableNamePrefix}.published`(`Id`,`Version`,`Name`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`) VALUES(@Id,'{_options.Value.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            var message = new MediumMessage()
            {
                DbId = content.GetId(),
                Origin = content,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var po = new
            {
                Id = message.DbId,
                Name = name,
                Content = StringSerializer.Serialize(message.Origin),
                Retries = message.Retries,
                Added = message.Added,
                ExpiresAt = message.ExpiresAt,
                StatusName = StatusName.Scheduled
            };

            if (dbTransaction == null)
            {
                using (var connection = new MySqlConnection(_options.Value.ConnectionString))
                {
                    await connection.ExecuteAsync(sql, po);
                }
            }
            else
            {
                var dbTrans = dbTransaction as IDbTransaction;
                if (dbTrans == null && dbTransaction is IDbContextTransaction dbContextTrans)
                {
                    dbTrans = dbContextTrans.GetDbTransaction();
                }

                var conn = dbTrans?.Connection;
                await conn.ExecuteAsync(sql, po, dbTrans);
            }

            return message;
        }

        public async Task<MediumMessage> StoreMessageAsync(string name, string group, Message content, CancellationToken cancellationToken = default)
        {
            var sql = $@"INSERT INTO `{_options.Value.TableNamePrefix}.received`(`Id`,`Version`,`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`) VALUES(@Id,'{_options.Value.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            var message = new MediumMessage()
            {
                DbId = SnowflakeId.Default().NextId().ToString(),
                Origin = content,
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            var po = new
            {
                Id = message.DbId,
                Group = group,
                Name = name,
                Content = StringSerializer.Serialize(message.Origin),
                Retries = message.Retries,
                Added = message.Added,
                ExpiresAt = message.ExpiresAt,
                StatusName = StatusName.Scheduled
            };

            using (var connection = new MySqlConnection(_options.Value.ConnectionString))
            {
                await connection.ExecuteAsync(sql, po);
            }
            return message;
        }

        public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default)
        {
            using (var connection = new MySqlConnection(_options.Value.ConnectionString))
            {
                return await connection.ExecuteAsync(
                    $@"DELETE FROM `{table}` WHERE ExpiresAt < @timeout limit @batchCount;",
                    new { timeout, batchCount });
            }
        }

        public async Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql = $"SELECT * FROM `{_options.Value.TableNamePrefix}.published` WHERE `Retries`<{_capOptions.Value.FailedRetryCount} AND `Version`='{_capOptions.Value.Version}' AND `Added`<'{fourMinAgo}' AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";

            var result = new List<MediumMessage>();
            using (var connection = new MySqlConnection(_options.Value.ConnectionString))
            {
                var reader = await connection.ExecuteReaderAsync(sql);
                while (reader.Read())
                {
                    result.Add(new MediumMessage()
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = StringSerializer.DeSerialize(reader.GetString(3)),
                        Retries = reader.GetInt32(4),
                        Added = reader.GetDateTime(5)
                    });
                }
            }
            return result;
        }

        public async Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT * FROM `{_options.Value.TableNamePrefix}.received` WHERE `Retries`<{_capOptions.Value.FailedRetryCount} AND `Version`='{_capOptions.Value.Version}' AND `Added`<'{fourMinAgo}' AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";

            var result = new List<MediumMessage>();
            using (var connection = new MySqlConnection(_options.Value.ConnectionString))
            {
                var reader = await connection.ExecuteReaderAsync(sql);
                while (reader.Read())
                {
                    result.Add(new MediumMessage()
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = StringSerializer.DeSerialize(reader.GetString(3)),
                        Retries = reader.GetInt32(4),
                        Added = reader.GetDateTime(5)
                    });
                }
            }
            return result;
        }

        //public Task<CapPublishedMessage> GetPublishedMessageAsync(long id)
        //{
        //    var sql = $@"SELECT * FROM `{_prefix}.published` WHERE `Id`={id};";

        //    using (var connection = new MySqlConnection(Options.ConnectionString))
        //    {
        //        return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
        //    }
        //}

        //public Task<CapReceivedMessage> GetReceivedMessageAsync(long id)
        //{
        //    var sql =
        //        $@"SELECT * FROM `{_prefix}.received` WHERE Id={id};";
        //    using (var connection = new MySqlConnection(Options.ConnectionString))
        //    {
        //        return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
        //    }
        //}
    }
}

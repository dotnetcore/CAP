using System;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetCore.CAP.MySql.Test
{
    [Collection("MySql")]
    public class MySqlStorageConnectionTest : DatabaseTestHost
    {
        private MySqlStorageConnection _storage;

        public MySqlStorageConnectionTest()
        {
            var options = GetService<IOptions<MySqlOptions>>();
            var capOptions = GetService<IOptions<CapOptions>>();
            _storage = new MySqlStorageConnection(options, capOptions);
        }

        [Fact]
        public async Task GetPublishedMessageAsync_Test()
        {
            var sql = "INSERT INTO `cap.published`(`Id`,`Version`,`Name`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`) VALUES(@Id,'v1',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
            var insertedId = SnowflakeId.Default().NextId();
            var publishMessage = new CapPublishedMessage
            {
                Id = insertedId,
                Name = "MySqlStorageConnectionTest",
                Content = "",
                StatusName = StatusName.Scheduled
            };
            using (var connection = ConnectionUtil.CreateConnection())
            {
                await connection.ExecuteAsync(sql, publishMessage);
            }
            var message = await _storage.GetPublishedMessageAsync(insertedId);
            Assert.NotNull(message);
            Assert.Equal("MySqlStorageConnectionTest", message.Name);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
        }

        [Fact]
        public void StoreReceivedMessageAsync_Test()
        {
            var receivedMessage = new CapReceivedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                Name = "MySqlStorageConnectionTest",
                Content = "",
                Group = "mygroup",
                StatusName = StatusName.Scheduled
            };

            Exception exception = null;
            try
            {
                _storage.StoreReceivedMessage(receivedMessage);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.Null(exception);
        }

        [Fact]
        public async Task GetReceivedMessageAsync_Test()
        {
            var sql = $@"
        INSERT INTO `cap.received`(`Id`,`Version`,`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)
        VALUES(@Id,'v1',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
            var insertedId = SnowflakeId.Default().NextId();
            var receivedMessage = new CapReceivedMessage
            {
                Id = insertedId,
                Name = "MySqlStorageConnectionTest",
                Content = "",
                Group = "mygroup",
                StatusName = StatusName.Scheduled
            };
            using (var connection = ConnectionUtil.CreateConnection())
            {
                await connection.ExecuteAsync(sql, receivedMessage);
            }

            var message = await _storage.GetReceivedMessageAsync(insertedId);
            Assert.NotNull(message);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
            Assert.Equal("MySqlStorageConnectionTest", message.Name);
            Assert.Equal("mygroup", message.Group);
        }
    }
}
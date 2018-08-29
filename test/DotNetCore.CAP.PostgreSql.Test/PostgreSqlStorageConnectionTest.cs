using System;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Xunit;

namespace DotNetCore.CAP.PostgreSql.Test
{
    [Collection("postgresql")]
    public class PostgreSqlStorageConnectionTest : DatabaseTestHost
    {
        private PostgreSqlStorageConnection _storage;

        public PostgreSqlStorageConnectionTest()
        {
            var options = GetService<PostgreSqlOptions>();
            var capOptions = GetService<CapOptions>();
            _storage = new PostgreSqlStorageConnection(options,capOptions);
        }

        [Fact]
        public async Task GetPublishedMessageAsync_Test()
        {
            var sql = @"INSERT INTO ""cap"".""published""(""Id"",""Name"",""Content"",""Retries"",""Added"",""ExpiresAt"",""StatusName"") VALUES(@Id,@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
            var insertedId = SnowflakeId.Default().NextId();
            var publishMessage = new CapPublishedMessage
            {
                Id = insertedId,
                Name = "PostgreSqlStorageConnectionTest",
                Content = "",
                StatusName = StatusName.Scheduled
            };
            using (var connection = ConnectionUtil.CreateConnection())
            {
                await connection.ExecuteAsync(sql, publishMessage);
            }
            var message = await _storage.GetPublishedMessageAsync(insertedId);
            Assert.NotNull(message);
            Assert.Equal("PostgreSqlStorageConnectionTest", message.Name);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
        }

        [Fact]
        public void StoreReceivedMessageAsync_Test()
        {
            var receivedMessage = new CapReceivedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                Name = "PostgreSqlStorageConnectionTest",
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
        INSERT INTO ""cap"".""received""(""Id"",""Name"",""Group"",""Content"",""Retries"",""Added"",""ExpiresAt"",""StatusName"")
        VALUES(@Id,@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
            var insertedId = SnowflakeId.Default().NextId();
            var receivedMessage = new CapReceivedMessage
            {
                Id= insertedId,
                Name = "PostgreSqlStorageConnectionTest",
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
            Assert.Equal("PostgreSqlStorageConnectionTest", message.Name);
            Assert.Equal("mygroup", message.Group);
        }
    }
}
using System;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Xunit;

namespace DotNetCore.CAP.SqlServer.Test
{
    [Collection("sqlserver")]
    public class SqlServerStorageConnectionTest : DatabaseTestHost
    {
        private readonly SqlServerStorageConnection _storage;

        public SqlServerStorageConnectionTest()
        {
            _storage = new SqlServerStorageConnection(SqlSeverOptions, CapOptions);
        }

        [Fact]
        public async Task GetPublishedMessageAsync_Test()
        {
            var sql = "INSERT INTO [Cap].[Published]([Id],[Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName]) VALUES(@Id,@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
            var insertedId = SnowflakeId.Default().NextId();
            var publishMessage = new CapPublishedMessage
            {
                Id= insertedId,
                Name = "SqlServerStorageConnectionTest",
                Content = "",
                StatusName = StatusName.Scheduled
            };
           
            using (var connection = ConnectionUtil.CreateConnection())
            {
               await connection.ExecuteAsync(sql, publishMessage);
            }

            var message = await _storage.GetPublishedMessageAsync(insertedId);
            Assert.NotNull(message);
            Assert.Equal("SqlServerStorageConnectionTest", message.Name);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
        }
         
        [Fact]
        public void StoreReceivedMessageAsync_Test()
        {
            var receivedMessage = new CapReceivedMessage
            {
                Name = "SqlServerStorageConnectionTest",
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
            var sql = @"INSERT INTO [Cap].[Received]([Id],[Name],[Group],[Content],[Retries],[Added],[ExpiresAt],[StatusName]) VALUES(@Id,@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
            var insertedId = SnowflakeId.Default().NextId();
            var receivedMessage = new CapReceivedMessage
            {
                Id= insertedId,
                Name = "SqlServerStorageConnectionTest",
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
            Assert.Equal("SqlServerStorageConnectionTest", message.Name);
            Assert.Equal("mygroup", message.Group);
        }
    }
}
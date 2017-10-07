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
            var sql = @"INSERT INTO ""cap"".""published""(""Name"",""Content"",""Retries"",""Added"",""ExpiresAt"",""StatusName"") VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING ""Id"";";
            var publishMessage = new CapPublishedMessage
            {
                Name = "PostgreSqlStorageConnectionTest",
                Content = "",
                StatusName = StatusName.Scheduled
            };
            var insertedId = default(int);
            using (var connection = ConnectionUtil.CreateConnection())
            {
                insertedId = connection.QueryFirst<int>(sql, publishMessage);
            }
            var message = await _storage.GetPublishedMessageAsync(insertedId);
            Assert.NotNull(message);
            Assert.Equal("PostgreSqlStorageConnectionTest", message.Name);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
        }

        [Fact]
        public async Task FetchNextMessageAsync_Test()
        {
            var sql = @"INSERT INTO ""cap"".""queue""(""MessageId"",""MessageType"") VALUES(@MessageId,@MessageType);";
            var queue = new CapQueue
            {
                MessageId = 3333,
                MessageType = MessageType.Publish
            };
            using (var connection = ConnectionUtil.CreateConnection())
            {
                connection.Execute(sql, queue);
            }
            var fetchedMessage = await _storage.FetchNextMessageAsync();
            fetchedMessage.Dispose();
            Assert.NotNull(fetchedMessage);
            Assert.Equal(MessageType.Publish, fetchedMessage.MessageType);
            Assert.Equal(3333, fetchedMessage.MessageId);
        }

        [Fact]
        public async Task StoreReceivedMessageAsync_Test()
        {
            var receivedMessage = new CapReceivedMessage
            {
                Name = "PostgreSqlStorageConnectionTest",
                Content = "",
                Group = "mygroup",
                StatusName = StatusName.Scheduled
            };

            Exception exception = null;
            try
            {
                await _storage.StoreReceivedMessageAsync(receivedMessage);
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
        INSERT INTO ""cap"".""received""(""Name"",""Group"",""Content"",""Retries"",""Added"",""ExpiresAt"",""StatusName"")
        VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING ""Id"";";
            var receivedMessage = new CapReceivedMessage
            {
                Name = "PostgreSqlStorageConnectionTest",
                Content = "",
                Group = "mygroup",
                StatusName = StatusName.Scheduled
            };
            var insertedId = default(int);
            using (var connection = ConnectionUtil.CreateConnection())
            {
                insertedId = connection.QueryFirst<int>(sql, receivedMessage);
            }

            var message = await _storage.GetReceivedMessageAsync(insertedId);

            Assert.NotNull(message);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
            Assert.Equal("PostgreSqlStorageConnectionTest", message.Name);
            Assert.Equal("mygroup", message.Group);
        }

        [Fact]
        public async Task GetNextReceviedMessageToBeEnqueuedAsync_Test()
        {
            var receivedMessage = new CapReceivedMessage
            {
                Name = "PostgreSqlStorageConnectionTest",
                Content = "",
                Group = "mygroup",
                StatusName = StatusName.Scheduled
            };
            await _storage.StoreReceivedMessageAsync(receivedMessage);

            var message = await _storage.GetNextReceivedMessageToBeEnqueuedAsync();

            Assert.NotNull(message);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
            Assert.Equal("PostgreSqlStorageConnectionTest", message.Name);
            Assert.Equal("mygroup", message.Group);
        }
    }
}
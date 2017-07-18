using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Xunit;

namespace DotNetCore.CAP.SqlServer.Test
{
    public class SqlServerStorageConnectionTest : DatabaseTestHost
    {
        private SqlServerStorageConnection _storage;

        public SqlServerStorageConnectionTest()
        {
            var options = GetService<SqlServerOptions>();
            _storage = new SqlServerStorageConnection(options);
        }

        [Fact]
        public async void GetPublishedMessageAsync_Test()
        {
            var sql = "INSERT INTO [Cap].[Published]([Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName]) OUTPUT INSERTED.Id VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
            var publishMessage = new CapPublishedMessage
            {
                Name = "SqlServerStorageConnectionTest",
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
            Assert.Equal("SqlServerStorageConnectionTest", message.Name);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
        }

        [Fact]
        public async void FetchNextMessageAsync_Test()
        {
            var sql = "INSERT INTO [Cap].[Queue]([MessageId],[MessageType]) VALUES(@MessageId,@MessageType);";
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
        public async void StoreReceivedMessageAsync_Test()
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
                await _storage.StoreReceivedMessageAsync(receivedMessage);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.Null(exception);
        }

        [Fact]
        public async void GetReceivedMessageAsync_Test()
        {

            var sql = $@"
INSERT INTO [Cap].[Received]([Name],[Group],[Content],[Retries],[Added],[ExpiresAt],[StatusName]) OUTPUT INSERTED.Id
VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
            var receivedMessage = new CapReceivedMessage
            {
                Name = "SqlServerStorageConnectionTest",
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
            Assert.Equal("SqlServerStorageConnectionTest", message.Name);
            Assert.Equal("mygroup", message.Group);
        }

        [Fact]
        public async void GetNextReceviedMessageToBeEnqueuedAsync_Test()
        {
            var receivedMessage = new CapReceivedMessage
            {
                Name = "SqlServerStorageConnectionTest",
                Content = "",
                Group = "mygroup",
                StatusName = StatusName.Scheduled
            };
            await _storage.StoreReceivedMessageAsync(receivedMessage);

            var message = await _storage.GetNextReceviedMessageToBeEnqueuedAsync();

            Assert.NotNull(message);
            Assert.Equal(StatusName.Scheduled, message.StatusName);
            Assert.Equal("SqlServerStorageConnectionTest", message.Name);
            Assert.Equal("mygroup", message.Group);
        }

    }
}

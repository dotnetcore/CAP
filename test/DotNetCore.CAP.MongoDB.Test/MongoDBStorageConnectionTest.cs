using System;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace DotNetCore.CAP.MongoDB.Test
{
    [Collection("MongoDB")]
    public class MongoDBStorageConnectionTest : DatabaseTestHost
    {
        private IStorageConnection _connection =>
            Provider.GetService<MongoDBStorage>().GetConnection();

        [Fact]
        public void StoreReceivedMessageAsync_TestAsync()
        {
            var messageContext = new MessageContext
            {
                Group = "test",
                Name = "test",
                Content = "test-content"
            };

            _connection.StoreReceivedMessage(new ReceivedMessage()
            {
                Id = SnowflakeId.Default().NextId(),
                Group=messageContext.Group,
                Content=messageContext.Content,
                Name=messageContext.Name,
                Version="v1"
            });
        }

        [Fact]
        public void ChangeReceivedState_Test()
        {
            StoreReceivedMessageAsync_TestAsync();
            var collection = Database.GetCollection<ReceivedMessage>(MongoDBOptions.Value.ReceivedCollection);

            var msg = collection.Find(x => true).FirstOrDefault();
            _connection.ChangeReceivedState(msg.Id, StatusName.Scheduled).Should().BeTrue();
            collection.Find(x => x.Id == msg.Id).FirstOrDefault()?.StatusName.Should().Be(StatusName.Scheduled);
        }

        [Fact]
        public async void GetReceivedMessagesOfNeedRetry_TestAsync()
        {
            var msgs = await _connection.GetReceivedMessagesOfNeedRetry();

            msgs.Should().BeEmpty();

            var id = SnowflakeId.Default().NextId();

            var msg = new CapReceivedMessage
            {
                Id = id,
                Group = "test",
                Name = "test",
                Content = "test-content",
                StatusName = StatusName.Failed
            };
            _connection.StoreReceivedMessage(msg);

            var collection = Database.GetCollection<ReceivedMessage>(MongoDBOptions.Value.ReceivedCollection);

            var updateDef = Builders<ReceivedMessage>
                .Update.Set(x => x.Added, DateTime.Now.AddMinutes(-5));

            await collection.UpdateOneAsync(x => x.Id == id, updateDef);

            msgs = await _connection.GetReceivedMessagesOfNeedRetry();
            msgs.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void GetReceivedMessageAsync_Test()
        {
            var msg = _connection.GetReceivedMessageAsync(1);
            msg.Should().NotBeNull();
        }
    }
}
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
        public async void StoreReceivedMessageAsync_TestAsync()
        {
            var id = await _connection.StoreReceivedMessageAsync(new CapReceivedMessage(new MessageContext
            {
                Group = "test",
                Name = "test",
                Content = "test-content"
            }));

            id.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ChangeReceivedState_Test()
        {
            StoreReceivedMessageAsync_TestAsync();
            var collection = Database.GetCollection<CapReceivedMessage>(MongoDBOptions.ReceivedCollection);

            var msg = collection.Find(x => true).FirstOrDefault();
            _connection.ChangeReceivedState(msg.Id, StatusName.Scheduled).Should().BeTrue();
            collection.Find(x => x.Id == msg.Id).FirstOrDefault()?.StatusName.Should().Be(StatusName.Scheduled);
        }

        [Fact]
        public async void GetReceivedMessagesOfNeedRetry_TestAsync()
        {
            var msgs = await _connection.GetReceivedMessagesOfNeedRetry();

            msgs.Should().BeEmpty();

            var msg = new CapReceivedMessage
            {
                Group = "test",
                Name = "test",
                Content = "test-content",
                StatusName = StatusName.Failed
            };
            var id = await _connection.StoreReceivedMessageAsync(msg);

            var collection = Database.GetCollection<CapReceivedMessage>(MongoDBOptions.ReceivedCollection);

            var updateDef = Builders<CapReceivedMessage>
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
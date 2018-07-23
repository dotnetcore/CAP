using System.Threading;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Xunit;
using Xunit.Priority;

namespace DotNetCore.CAP.MongoDB.Test
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class MongoDBStorageConnectionTest
    {
        private readonly MongoClient _client;
        private readonly MongoDBOptions _options;
        private readonly MongoDBStorage _storage;
        private readonly IStorageConnection _connection;

        public MongoDBStorageConnectionTest()
        {
            _client = new MongoClient(ConnectionUtil.ConnectionString);
            _options = new MongoDBOptions();
            _storage = new MongoDBStorage(new CapOptions(), _options, _client, NullLogger<MongoDBStorage>.Instance);
            _connection = _storage.GetConnection();
        }

        [Fact, Priority(1)]
        public async void StoreReceivedMessageAsync_TestAsync()
        {
            await _storage.InitializeAsync(default(CancellationToken));

            var id = await
            _connection.StoreReceivedMessageAsync(new CapReceivedMessage(new MessageContext
            {
                Group = "test",
                Name = "test",
                Content = "test-content"
            }));
            id.Should().BeGreaterThan(0);
        }

        [Fact, Priority(2)]
        public void ChangeReceivedState_Test()
        {
            var collection = _client.GetDatabase(_options.Database).GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            var msg = collection.Find(x => true).FirstOrDefault();
            _connection.ChangeReceivedState(msg.Id, StatusName.Scheduled).Should().BeTrue();
            collection.Find(x => x.Id == msg.Id).FirstOrDefault()?.StatusName.Should().Be(StatusName.Scheduled);
        }

        [Fact, Priority(3)]
        public async void GetReceivedMessagesOfNeedRetry_TestAsync()
        {
            var msgs = await _connection.GetReceivedMessagesOfNeedRetry();
            msgs.Should().HaveCountGreaterThan(0);
        }

        [Fact, Priority(4)]
        public void GetReceivedMessageAsync_Test()
        {
            var msg = _connection.GetReceivedMessageAsync(1);
            msg.Should().NotBeNull();
        }
    }
}
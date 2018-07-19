using System.Threading;
using DotNetCore.CAP.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Xunit;

namespace DotNetCore.CAP.MongoDB.Test
{
    public class MongoDBStorageConnectionTest
    {
        private MongoClient _client;
        private MongoDBStorage _storage;

        public MongoDBStorageConnectionTest()
        {
            _client = new MongoClient(ConnectionUtil.ConnectionString);
            var options = new MongoDBOptions();
            _storage = new MongoDBStorage(new CapOptions(), options, _client, NullLogger<MongoDBStorage>.Instance);
        }

        [Fact]
        public async void StoreReceivedMessageAsync_TestAsync()
        {
            await _storage.InitializeAsync(default(CancellationToken));
            var connection = _storage.GetConnection();

            var id = await
            connection.StoreReceivedMessageAsync(new CapReceivedMessage(new MessageContext
            {
                Group = "test",
                Name = "test",
                Content = "test-content"
            }));
            id.Should().BeGreaterThan(0);
        }
    }
}
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace DotNetCore.CAP.MongoDB.Test
{
    public class MongoDBStorageTest
    {
        private MongoClient _client;

        public MongoDBStorageTest()
        {
            _client = new MongoClient(CommonHelper.ConnectionString);
        }

        [Fact]
        public async void InitializeAsync_Test()
        {
            var options = new MongoDBOptions();
            var storage = new MongoDBStorage(new CapOptions(), options, _client, NullLogger<MongoDBStorage>.Instance);
            await storage.InitializeAsync(default(CancellationToken));
            var names = _client.ListDatabaseNames()?.ToList();
            names.Should().Contain(options.Database);

            var collections = _client.GetDatabase(options.Database).ListCollectionNames()?.ToList();
            collections.Should().Contain(options.Published);
            collections.Should().Contain(options.Received);
            collections.Should().Contain("Counter");

            var collection = _client.GetDatabase(options.Database).GetCollection<BsonDocument>("Counter");
            collection.CountDocuments(new BsonDocument { { "_id", options.Published } }).Should().Be(1);
            collection.CountDocuments(new BsonDocument { { "_id", options.Received } }).Should().Be(1);
        }
    }
}
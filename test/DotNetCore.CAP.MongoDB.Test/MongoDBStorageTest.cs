using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace DotNetCore.CAP.MongoDB.Test
{
    [Collection("MongoDB")]
    public class MongoDBStorageTest : DatabaseTestHost
    {
        [Fact]
        public void InitializeAsync_Test()
        {
            var storage = Provider.GetService<MongoDBStorage>();
            var names = MongoClient.ListDatabaseNames()?.ToList();
            names.Should().Contain(MongoDBOptions.DatabaseName);

            var collections = Database.ListCollectionNames()?.ToList();
            collections.Should().Contain(MongoDBOptions.PublishedCollection);
            collections.Should().Contain(MongoDBOptions.ReceivedCollection);
            collections.Should().Contain(MongoDBOptions.CounterCollection);

            var collection = Database.GetCollection<BsonDocument>(MongoDBOptions.CounterCollection);
            collection.CountDocuments(new BsonDocument { { "_id", MongoDBOptions.PublishedCollection } }).Should().Be(1);
            collection.CountDocuments(new BsonDocument { { "_id", MongoDBOptions.ReceivedCollection } }).Should().Be(1);
        }
    }
}
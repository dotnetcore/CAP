using FluentAssertions;
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
            var names = MongoClient.ListDatabaseNames()?.ToList();
            names.Should().Contain(MongoDBOptions.Value.DatabaseName);

            var collections = Database.ListCollectionNames()?.ToList();
            collections.Should().Contain(MongoDBOptions.Value.PublishedCollection);
            collections.Should().Contain(MongoDBOptions.Value.ReceivedCollection);
        }
    }
}
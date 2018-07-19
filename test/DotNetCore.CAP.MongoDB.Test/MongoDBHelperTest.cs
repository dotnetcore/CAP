using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace DotNetCore.CAP.MongoDB.Test
{
    public class MongoDBHelperTest
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        string _recieved = "ReceivedTest";

        public MongoDBHelperTest()
        {
            _client = new MongoClient(ConnectionUtil.ConnectionString);
            _database = _client.GetDatabase("CAP_Test");

            //Initialize MongoDB
            if (!_database.ListCollectionNames().ToList().Any(x => x == "Counter"))
            {
                var collection = _database.GetCollection<BsonDocument>("Counter");
                collection.InsertOne(new BsonDocument { { "_id", _recieved }, { "sequence_value", 0 } });
            }
        }

        [Fact]
        public async void GetNextSequenceValueAsync_Test()
        {
            var id = await new MongoDBUtil().GetNextSequenceValueAsync(_database, _recieved);
            id.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetNextSequenceValue_Concurrency_Test()
        {
            var dic = new ConcurrentDictionary<int, int>();
            Parallel.For(0, 30, (x) =>
             {
                 var id = new MongoDBUtil().GetNextSequenceValue(_database, _recieved);
                 id.Should().BeGreaterThan(0);
                 dic.TryAdd(id, x).Should().BeTrue(); //The id shouldn't be same.
             });
        }
    }
}
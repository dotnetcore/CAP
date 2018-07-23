using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DotNetCore.CAP.MongoDB.Test
{
    [Collection("MongoDB")]
    public class MongoDBUtilTest : DatabaseTestHost
    {
        [Fact]
        public async void GetNextSequenceValueAsync_Test()
        {
            var id = await new MongoDBUtil().GetNextSequenceValueAsync(Database, MongoDBOptions.ReceivedCollection);
            id.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetNextSequenceValue_Concurrency_Test()
        {
            var dic = new ConcurrentDictionary<int, int>();
            Parallel.For(0, 30, (x) =>
             {
                 var id = new MongoDBUtil().GetNextSequenceValue(Database, MongoDBOptions.ReceivedCollection);
                 id.Should().BeGreaterThan(0);
                 dic.TryAdd(id, x).Should().BeTrue(); //The id shouldn't be same.
             });
        }
    }
}
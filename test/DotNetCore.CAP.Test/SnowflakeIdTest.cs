using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class SnowflakeIdTest
    {
        [Fact]
        public void NextIdTest()
        {
            var instance = SnowflakeId.Default();
            var result = instance.NextId();
            var result2 = instance.NextId();

            Assert.True(result2 - result == 1);
        }

        [Fact]
        public void ConcurrentNextIdTest()
        {
            var array = new long[1000];

            Parallel.For(0, 1000, i =>
            {
                var id = SnowflakeId.Default().NextId();
                array[i] = id;
            });

            Assert.True(array.Distinct().Count() == 1000);
        }

        [Fact]
        public void TestNegativeWorkerId()
        {
            Assert.Throws<ArgumentException>(() => new SnowflakeId(-1));
        }

        [Fact]
        public void TestTooLargeWorkerId()
        {
            Assert.Throws<ArgumentException>(() => new SnowflakeId(1024));
        }
    }
}

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
            var result = SnowflakeId.Default().NextId();

            Assert.IsType<long>(result);
            Assert.True(result > 0);
            Assert.True(result.ToString().Length == long.MaxValue.ToString().Length);
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

    }
}

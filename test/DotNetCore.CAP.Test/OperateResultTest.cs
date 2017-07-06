using System.Linq;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class OperateResultTest
    {
        [Fact]
        public void VerifyDefaultConstructor()
        {
            var result = new OperateResult();

            Assert.False(result.Succeeded);
            Assert.Equal(0, result.Errors.Count());
        }

        [Fact]
        public void NullFaildUsesEmptyErrors()
        {
            var result = OperateResult.Failed();

            Assert.False(result.Succeeded);
            Assert.Equal(0, result.Errors.Count());
        }
    }
}
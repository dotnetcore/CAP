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
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void NullFaildUsesEmptyErrors()
        {
            var result = OperateResult.Failed();

            Assert.False(result.Succeeded);
            Assert.Empty(result.Errors);
        }
    }
}
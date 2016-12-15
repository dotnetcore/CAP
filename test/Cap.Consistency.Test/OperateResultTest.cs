using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Cap.Consistency.Test
{
    public class OperateResultTest
    {
        [Fact]
        public void VerifyDefaultConstructor() {
            var result = new OperateResult();

            Assert.False(result.Succeeded);
            Assert.Equal(0, result.Errors.Count());
        }

        [Fact]
        public void NullFaildUsesEmptyErrors() {
            var result = OperateResult.Failed();

            Assert.False(result.Succeeded);
            Assert.Equal(0, result.Errors.Count());
        }
    }
}

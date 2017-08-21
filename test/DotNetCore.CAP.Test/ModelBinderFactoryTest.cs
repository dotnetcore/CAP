using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Internal;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ModelBinderFactoryTest
    {
        IModelBinderFactory _factory;

        public ModelBinderFactoryTest()
        {
            _factory = new ModelBinderFactory();
        }

        [Theory]
        [InlineData(nameof(Sample.DateTimeParam))]
        [InlineData(nameof(Sample.StringParam))]
        [InlineData(nameof(Sample.IntegerParam))]
        [InlineData(nameof(Sample.GuidParam))]
        [InlineData(nameof(Sample.UriParam))]
        public void CreateSimpleTypeBinderTest(string methodName)
        {
            var methodInfo = typeof(Sample).GetRuntimeMethods().Single(x => x.Name == methodName);
            var binder = _factory.CreateBinder(methodInfo.GetParameters()[0]);
            Assert.NotNull(binder);
            Assert.True(binder is SimpleTypeModelBinder);
            Assert.False(binder is ComplexTypeModelBinder);
        }

        [Theory]
        [InlineData(nameof(Sample.ComplexTypeParam))]
        public void CreateComplexTypeBinderTest(string methodName)
        {
            var methodInfo = typeof(Sample).GetRuntimeMethods().Single(x => x.Name == methodName);
            var binder = _factory.CreateBinder(methodInfo.GetParameters()[0]);
            Assert.NotNull(binder);
            Assert.False(binder is SimpleTypeModelBinder);
            Assert.True(binder is ComplexTypeModelBinder);
        }

    }
}

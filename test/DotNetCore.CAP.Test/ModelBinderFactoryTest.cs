using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Internal;
using Moq;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ModelBinderFactoryTest
    {
        private IModelBinderFactory _factory;

        public ModelBinderFactoryTest()
        {
            var serializer = Mock.Of<IContentSerializer>();
            _factory = new ModelBinderFactory(serializer);
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
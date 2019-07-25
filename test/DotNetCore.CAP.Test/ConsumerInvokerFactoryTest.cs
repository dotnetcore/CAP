using System;
using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ConsumerInvokerFactoryTest
    {
        private IServiceProvider _serviceProvider;

        private Mock<IContentSerializer> _mockSerialiser;
        private Mock<IMessagePacker> _mockMessagePacker;
        private Mock<IModelBinderFactory> _mockModelBinderFactory;

        public ConsumerInvokerFactoryTest()
        {
            _mockSerialiser = new Mock<IContentSerializer>();
            _mockMessagePacker = new Mock<IMessagePacker>();
            _mockModelBinderFactory = new Mock<IModelBinderFactory>();

            var services = new ServiceCollection();
            services.AddSingleton<ConsumerInvokerFactory>();

            services.AddLogging();
            services.AddSingleton(_mockSerialiser.Object);
            services.AddSingleton(_mockMessagePacker.Object);
            services.AddSingleton(_mockModelBinderFactory.Object);
            _serviceProvider = services.BuildServiceProvider();
        }

        private ConsumerInvokerFactory Create() =>
            _serviceProvider.GetService<ConsumerInvokerFactory>();

        [Fact]
        public void CreateInvokerTest()
        {
            // Arrange
            var fixure = Create();

            // Act
            var invoker = fixure.CreateInvoker();

            // Assert
            Assert.NotNull(invoker);
        }

        [Theory]
        [InlineData(nameof(Sample.ThrowException))]
        [InlineData(nameof(Sample.AsyncMethod))]
        public void InvokeMethodTest(string methodName)
        {
            // Arrange
            var fixure = Create();

            var methodInfo = typeof(Sample).GetRuntimeMethods()
                .Single(x => x.Name == methodName);

            var description = new ConsumerExecutorDescriptor
            {
                MethodInfo = methodInfo,
                ImplTypeInfo = typeof(Sample).GetTypeInfo()
            };
            var messageContext = new MessageContext();

            var context = new ConsumerContext(description, messageContext);

            var invoker = fixure.CreateInvoker();

            Assert.Throws<Exception>(() =>
            {
                invoker.InvokeAsync(context).GetAwaiter().GetResult();
            });
        }
    }
}
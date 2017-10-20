using System;
using System.Linq;
using System.Reflection;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ConsumerInvokerFactoryTest
    {
        private IConsumerInvokerFactory consumerInvokerFactory;

        public ConsumerInvokerFactoryTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IContentSerializer, JsonContentSerializer>();
            var provider = services.BuildServiceProvider();
            var logFactory = provider.GetRequiredService<ILoggerFactory>();
            var mesagePacker = provider.GetRequiredService<IMessagePacker>();
            var binder = new ModelBinderFactory();

            consumerInvokerFactory = new ConsumerInvokerFactory(logFactory, mesagePacker,binder,  provider);
        }

        [Fact]
        public void CreateInvokerTest()
        {
            var methodInfo = typeof(Sample).GetRuntimeMethods()
               .Single(x => x.Name == nameof(Sample.ThrowException));

            var description = new ConsumerExecutorDescriptor
            {
                MethodInfo = methodInfo,
                ImplTypeInfo = typeof(Sample).GetTypeInfo()
            };
            var messageContext = new MessageContext();

            var context = new ConsumerContext(description, messageContext);

            var invoker = consumerInvokerFactory.CreateInvoker(context);

            Assert.NotNull(invoker);
        }

        [Theory]
        [InlineData(nameof(Sample.ThrowException))]
        [InlineData(nameof(Sample.AsyncMethod))]
        public void InvokeMethodTest(string methodName)
        {
            var methodInfo = typeof(Sample).GetRuntimeMethods()
                .Single(x => x.Name == methodName);

            var description = new ConsumerExecutorDescriptor
            {
                MethodInfo = methodInfo,
                ImplTypeInfo = typeof(Sample).GetTypeInfo()
            };
            var messageContext = new MessageContext();

            var context = new ConsumerContext(description, messageContext);

            var invoker = consumerInvokerFactory.CreateInvoker(context);

            Assert.Throws<Exception>(() =>
            {
                invoker.InvokeAsync().GetAwaiter().GetResult();
            }); 
        }
    }
}
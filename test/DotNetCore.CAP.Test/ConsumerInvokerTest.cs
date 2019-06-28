using System;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ConsumerInvokerTest
    {
        private ILoggerFactory _loggerFactory;
        private Mock<IMessagePacker> _mockMessagePacker;
        private Mock<IModelBinderFactory> _mockModelBinderFactory;
        private MessageContext _messageContext;

        public ConsumerInvokerTest()
        {
            _loggerFactory = new NullLoggerFactory();
            _mockMessagePacker = new Mock<IMessagePacker>();
            _mockModelBinderFactory = new Mock<IModelBinderFactory>();
        }

        private Internal.DefaultConsumerInvoker InitDefaultConsumerInvoker(IServiceProvider provider)
        {
            var invoker = new Internal.DefaultConsumerInvoker(
             _loggerFactory,
             provider,
             _mockMessagePacker.Object,
             _mockModelBinderFactory.Object);

            var message = new CapReceivedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                Name = "test",
                Content = DateTime.Now.ToString(),
                StatusName = StatusName.Scheduled,
                Group = "Group.Test"
            };

            _mockMessagePacker
                .Setup(x => x.UnPack(It.IsAny<string>()))
                .Returns(new CapMessageDto(message.Content));

            _messageContext = new MessageContext
            {
                Group = message.Group,
                Name = message.Name,
                Content = Helper.ToJson(message)
            };

            return invoker;
        }

        [Fact]
        public async Task CanInvokeServiceTest()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ITestService, TestService2>();
            services.AddSingleton<ITestService, TestService>();

            var provider = services.BuildServiceProvider();

            var invoker = InitDefaultConsumerInvoker(provider);

            var descriptor = new ConsumerExecutorDescriptor
            {
                ServiceTypeInfo = typeof(ITestService).GetTypeInfo(),
                ImplTypeInfo = typeof(TestService).GetTypeInfo(),
                MethodInfo = typeof(TestService).GetMethod("Index")
            };

            descriptor.Attribute = descriptor.MethodInfo.GetCustomAttribute<TopicAttribute>(true);

            var context = new Internal.ConsumerContext(descriptor, _messageContext);

            var result = await invoker.InvokeAsync(context);

            Assert.NotNull(result);
            Assert.NotNull(result.Result);
            Assert.Equal("test", result.Result.ToString());
        }

        [Fact]
        public async Task CanInvokeControllerTest()
        {
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();
            var invoker = InitDefaultConsumerInvoker(provider);

            var descriptor = new ConsumerExecutorDescriptor
            {
                ServiceTypeInfo = typeof(HomeController).GetTypeInfo(),
                ImplTypeInfo = typeof(HomeController).GetTypeInfo(),
                MethodInfo = typeof(HomeController).GetMethod("Index")
            };

            descriptor.Attribute = descriptor.MethodInfo.GetCustomAttribute<TopicAttribute>(true);

            var context = new Internal.ConsumerContext(descriptor, _messageContext);

            var result = await invoker.InvokeAsync(context);

            Assert.NotNull(result);
            Assert.NotNull(result.Result);
            Assert.Equal("test", result.Result.ToString());
        }

    }

    public class HomeController
    {
        [CapSubscribe("test")]
        public string Index()
        {
            return "test";
        }
    }

    public interface ITestService { }

    public class TestService : ITestService, ICapSubscribe
    {
        [CapSubscribe("test")]
        public string Index()
        {
            return "test";
        }
    }

    public class TestService2 : ITestService
    {
        [CapSubscribe("test")]
        public string Index()
        {
            return "test2";
        }
    }

    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string name) : base(name)
        {
        }
    }
}
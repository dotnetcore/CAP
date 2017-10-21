using System;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;

namespace DotNetCore.CAP.Test
{
    public class QueueExecutorFactoryTest
    {
        private IServiceProvider _provider;

        public QueueExecutorFactoryTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();

            var callbackSender = Mock.Of<ICallbackMessageSender>();

            services.AddSingleton(callbackSender);
            services.AddCap(x => { });
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanCreateInstance()
        {
            var queueExecutorFactory = _provider.GetService<IQueueExecutorFactory>();
            Assert.NotNull(queueExecutorFactory);

            var publishExecutor = queueExecutorFactory.GetInstance(Models.MessageType.Publish);
            Assert.Null(publishExecutor);

            var disPatchExector = queueExecutorFactory.GetInstance(Models.MessageType.Subscribe);
            Assert.NotNull(disPatchExector);
        }

        [Fact]
        public void CanGetSubscribeExector()
        {
            var queueExecutorFactory = _provider.GetService<IQueueExecutorFactory>();
            Assert.NotNull(queueExecutorFactory);

            var publishExecutor = queueExecutorFactory.GetInstance(Models.MessageType.Publish);
            Assert.Null(publishExecutor);
        }
    }
}
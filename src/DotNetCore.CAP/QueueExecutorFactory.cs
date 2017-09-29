using System;
using System.Linq;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP
{
    public class QueueExecutorFactory : IQueueExecutorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QueueExecutorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IQueueExecutor GetInstance(MessageType messageType)
        {
            var queueExecutors = _serviceProvider.GetServices<IQueueExecutor>();

            return messageType == MessageType.Publish
                ? queueExecutors.FirstOrDefault(x => x is BasePublishQueueExecutor)
                : queueExecutors.FirstOrDefault(x => !(x is BasePublishQueueExecutor));
        }
    }
}
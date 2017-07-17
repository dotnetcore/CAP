using System;
using System.Linq;
using System.Reflection;
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
            var queueExectors = _serviceProvider.GetServices<IQueueExecutor>();

            return messageType == MessageType.Publish 
                ? queueExectors.FirstOrDefault(x => x is BasePublishQueueExecutor) 
                : queueExectors.FirstOrDefault(x => !(x is BasePublishQueueExecutor));
        }
    }
}
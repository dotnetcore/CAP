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
            var _queueExectors = _serviceProvider.GetServices<IQueueExecutor>();

            if (messageType == MessageType.Publish)
            {
                return _queueExectors.FirstOrDefault(x => typeof(BasePublishQueueExecutor).IsAssignableFrom(x.GetType()));
            }
            else
            {
                return _queueExectors.FirstOrDefault(x => !typeof(BasePublishQueueExecutor).IsAssignableFrom(x.GetType()));
            }
        }
    }
}
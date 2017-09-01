using System;
using DotNetCore.CAP.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    public class ConsumerInvokerFactory : IConsumerInvokerFactory
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IModelBinderFactory _modelBinderFactory;

        public ConsumerInvokerFactory(
            ILoggerFactory loggerFactory,
            IModelBinderFactory modelBinderFactory,
            IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<ConsumerInvokerFactory>();
            _modelBinderFactory = modelBinderFactory;
            _serviceProvider = serviceProvider;
        }

        public IConsumerInvoker CreateInvoker(ConsumerContext consumerContext)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = new ConsumerInvokerContext(consumerContext)
                {
                    Result = new DefaultConsumerInvoker(_logger, scope.ServiceProvider, _modelBinderFactory, consumerContext)
                };

                return context.Result;
            }
        }
    }
}
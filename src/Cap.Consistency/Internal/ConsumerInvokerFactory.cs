using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.Internal
{
    public class ConsumerInvokerFactory : IConsumerInvokerFactory
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ObjectMethodExecutor _executor;

        public ConsumerInvokerFactory(
            ILoggerFactory loggerFactory, 
            IServiceProvider serviceProvider,
            ObjectMethodExecutor executor) {

            _logger = loggerFactory.CreateLogger<ConsumerInvokerFactory>();
            _serviceProvider = serviceProvider;
            _executor = executor;
        }

        public IConsumerInvoker CreateInvoker(ConsumerContext consumerContext) {

            var context = new ConsumerInvokerContext(consumerContext);

            context.Result = new ConsumerInvoker(_logger, _serviceProvider,
                consumerContext, _executor);

            return context.Result;
        }
    }
}

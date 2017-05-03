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

        public ConsumerInvokerFactory(
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider) {

            _logger = loggerFactory.CreateLogger<ConsumerInvokerFactory>();
            _serviceProvider = serviceProvider;
        }

        public IConsumerInvoker CreateInvoker(ConsumerContext consumerContext) {

            var context = new ConsumerInvokerContext(consumerContext);

            context.Result = new ConsumerInvoker(_logger, _serviceProvider, consumerContext);

            return context.Result;
        }
    }
}

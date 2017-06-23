using System;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Abstractions.ModelBinding;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    public class ConsumerInvokerFactory : IConsumerInvokerFactory
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IModelBinder _modelBinder;

        public ConsumerInvokerFactory(
            ILoggerFactory loggerFactory,
            IModelBinder modelBinder,
            IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<ConsumerInvokerFactory>();
            _modelBinder = modelBinder;
            _serviceProvider = serviceProvider;
        }

        public IConsumerInvoker CreateInvoker(ConsumerContext consumerContext)
        {
            var context = new ConsumerInvokerContext(consumerContext);

            context.Result = new DefaultConsumerInvoker(_logger, _serviceProvider, _modelBinder, consumerContext);

            return context.Result;
        }
    }
}
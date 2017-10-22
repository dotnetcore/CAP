using System;
using DotNetCore.CAP.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    internal class ConsumerInvokerFactory : IConsumerInvokerFactory
    {
        private readonly ILogger _logger;
        private readonly IMessagePacker _messagePacker;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IServiceProvider _serviceProvider;

        public ConsumerInvokerFactory(
            ILoggerFactory loggerFactory,
            IMessagePacker messagePacker,
            IModelBinderFactory modelBinderFactory,
            IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<ConsumerInvokerFactory>();
            _messagePacker = messagePacker;
            _modelBinderFactory = modelBinderFactory;
            _serviceProvider = serviceProvider;
        }

        public IConsumerInvoker CreateInvoker()
        {
            return new DefaultConsumerInvoker(_logger, _serviceProvider, _messagePacker, _modelBinderFactory);
        }
    }
}
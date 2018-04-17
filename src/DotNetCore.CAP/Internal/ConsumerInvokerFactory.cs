// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    internal class ConsumerInvokerFactory : IConsumerInvokerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMessagePacker _messagePacker;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IServiceProvider _serviceProvider;

        public ConsumerInvokerFactory(
            ILoggerFactory loggerFactory,
            IMessagePacker messagePacker,
            IModelBinderFactory modelBinderFactory,
            IServiceProvider serviceProvider)
        {
            _loggerFactory = loggerFactory;
            _messagePacker = messagePacker;
            _modelBinderFactory = modelBinderFactory;
            _serviceProvider = serviceProvider;
        }

        public IConsumerInvoker CreateInvoker()
        {
            return new DefaultConsumerInvoker(_loggerFactory, _serviceProvider, _messagePacker, _modelBinderFactory);
        }
    }
}
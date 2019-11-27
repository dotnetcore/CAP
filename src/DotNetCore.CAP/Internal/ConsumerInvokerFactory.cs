// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    internal class ConsumerInvokerFactory : IConsumerInvokerFactory
    {
        private readonly ILoggerFactory _loggerFactory; 
        private readonly IServiceProvider _serviceProvider;

        public ConsumerInvokerFactory(
            ILoggerFactory loggerFactory, 
            IServiceProvider serviceProvider)
        {
            _loggerFactory = loggerFactory; 
            _serviceProvider = serviceProvider;
        }

        public IConsumerInvoker CreateInvoker()
        {
            return new DefaultConsumerInvoker(_loggerFactory, _serviceProvider);
        }
    }
}
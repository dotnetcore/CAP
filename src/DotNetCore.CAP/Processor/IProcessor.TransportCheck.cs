// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Processor
{
    public class TransportCheckProcessor : IProcessor
    {
        private readonly ILogger<TransportCheckProcessor> _logger;
        private readonly IConsumerRegister _register;
        private readonly TimeSpan _waitingInterval;

        public TransportCheckProcessor(ILogger<TransportCheckProcessor> logger, IConsumerRegister register)
        {
            _logger = logger;
            _register = register;
            _waitingInterval = TimeSpan.FromSeconds(30);
        }

        public virtual async Task ProcessAsync(ProcessingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ThrowIfStopping();

            _logger.LogDebug("Transport connection checking...");

            if (!_register.IsHealthy())
            {
                _logger.LogWarning("Transport connection is unhealthy, reconnection...");

                _register.ReStart();
            }
            else
            {
                _logger.LogDebug("Transport connection healthy!");
            }

            await context.WaitAsync(_waitingInterval);
        }
    }
}
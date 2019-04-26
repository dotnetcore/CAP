// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Processor
{
    public class TransportCheckProcessor : IProcessor
    {
        private readonly IConsumerRegister _register;
        private readonly TimeSpan _waitingInterval;

        public TransportCheckProcessor(IConsumerRegister register)
        {
            _register = register;
            _waitingInterval = TimeSpan.FromSeconds(30);
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!_register.IsHealthy())
            {
                _register.ReStart();
            }

            await context.WaitAsync(_waitingInterval);
        } 
    }
}
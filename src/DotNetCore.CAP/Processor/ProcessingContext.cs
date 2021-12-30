// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Processor
{
    public class ProcessingContext : IDisposable
    {
        private IServiceScope? _scope;

        private ProcessingContext(ProcessingContext other)
        {
            Provider = other.Provider;
            CancellationToken = other.CancellationToken;
        }

        public ProcessingContext(
            IServiceProvider provider,
            CancellationToken cancellationToken)
        {
            Provider = provider;
            CancellationToken = cancellationToken;
        }

        public IServiceProvider Provider { get; private set; }

        public CancellationToken CancellationToken { get; }

        public bool IsStopping => CancellationToken.IsCancellationRequested;

        public void Dispose()
        {
            _scope?.Dispose();
        }

        public void ThrowIfStopping()
        {
            CancellationToken.ThrowIfCancellationRequested();
        }

        public ProcessingContext CreateScope()
        {
            var serviceScope = Provider.CreateScope();

            return new ProcessingContext(this)
            {
                _scope = serviceScope,
                Provider = serviceScope.ServiceProvider
            };
        }

        public Task WaitAsync(TimeSpan timeout)
        {
            return Task.Delay(timeout, CancellationToken);
        }
    }
}
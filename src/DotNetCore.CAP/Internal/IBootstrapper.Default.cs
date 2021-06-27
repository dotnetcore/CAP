// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Default implement of <see cref="T:DotNetCore.CAP.Internal.IBootstrapper" />.
    /// </summary>
    internal class Bootstrapper : BackgroundService, IBootstrapper
    {
        private readonly ILogger<Bootstrapper> _logger;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public Bootstrapper(
            ILogger<Bootstrapper> logger,
            IStorageInitializer storage,
            IDispatcher dispatcher,
            IEnumerable<IProcessingServer> processors)
        {
            _logger = logger;
            Storage = storage;
            Dispatcher = dispatcher;
            Processors = processors;
        }

        private IStorageInitializer Storage { get; }
        public IDispatcher Dispatcher { get; }

        private IEnumerable<IProcessingServer> Processors { get; }

        public async Task BootstrapAsync()
        {
            _logger.LogDebug("### CAP background task is starting.");

            try
            {
                await Storage.InitializeAsync(_cts.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Initializing the storage structure failed!");
            }

            try
            {
                Dispatcher.Start(_cts.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Initializing the dispatcher failed!");
            }

            _cts.Token.Register(() =>
            {
                _logger.LogDebug("### CAP background task is stopping.");
                
                foreach (var item in Processors)
                {
                    try
                    {
                        item.Dispose();
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.ExpectedOperationCanceledException(ex);
                    }
                }
            });

            await BootstrapCoreAsync();

            _logger.LogInformation("### CAP started!");
        }

        protected virtual Task BootstrapCoreAsync()
        {
            foreach (var item in Processors)
            {
                _cts.Token.ThrowIfCancellationRequested();

                try
                {
                    item.Start(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    _logger.ProcessorsStartedError(ex);
                }
            }

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BootstrapAsync();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            
            await base.StopAsync(cancellationToken);
        }
    }
}
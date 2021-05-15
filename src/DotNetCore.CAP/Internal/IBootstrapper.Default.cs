// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
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

        public Bootstrapper(
            ILogger<Bootstrapper> logger,
            IStorageInitializer storage,
            IEnumerable<IProcessingServer> processors,
            IEnumerable<IBootstrapperCallback> bootstrapperCallbacks)
        {
            _logger = logger;
            Storage = storage;
            Processors = processors;
            BootstrapperCallbacks = bootstrapperCallbacks;
        }

        private IStorageInitializer Storage { get; }

        private IEnumerable<IProcessingServer> Processors { get; }

        private IEnumerable<IBootstrapperCallback> BootstrapperCallbacks { get; }

        public async Task BootstrapAsync(CancellationToken stoppingToken)
        {
            BootstrapperCallbacks?.BootStrappingStarted();

            _logger.LogDebug("### CAP background task is starting.");

            try
            {
                BootstrapperCallbacks?.StorageInitStarted();

                await Storage.InitializeAsync(stoppingToken);

                BootstrapperCallbacks?.StorageInitSuccess();
            }
            catch (Exception e)
            {
                BootstrapperCallbacks?.StorageInitFailed(e);

                _logger.LogError(e, "Initializing the storage structure failed!");
            }

            stoppingToken.Register(() =>
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

                BootstrapperCallbacks.OnStop();
            });

            await BootstrapCoreAsync();

            _logger.LogInformation("### CAP started!");

            BootstrapperCallbacks?.OnStart();
        }

        protected virtual Task BootstrapCoreAsync()
        {
            foreach (var item in Processors)
            {
                try
                {
                    item.Start();
                }
                catch (Exception ex)
                {
                    _logger.ProcessorsStartedError(ex);
                }
            }

            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BootstrapAsync(stoppingToken);
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Default implement of <see cref="T:DotNetCore.CAP.Internal.IBootstrapper" />.
    /// </summary>
    internal class Bootstrapper : BackgroundService, IBootstrapper
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Bootstrapper> _logger;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;
        private IEnumerable<IProcessingServer> _processors = default!;

        public Bootstrapper(IServiceProvider serviceProvider, ILogger<Bootstrapper> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task BootstrapAsync()
        {
            _logger.LogDebug("### CAP background task is starting.");

            CheckRequirement();

            try
            {
                _processors = _serviceProvider.GetServices<IProcessingServer>();

                await _serviceProvider.GetRequiredService<IStorageInitializer>().InitializeAsync(_cts.Token);
            }
            catch (Exception e)
            {
                if (e is InvalidOperationException)
                {
                    throw;
                }
                _logger.LogError(e, "Initializing the storage structure failed!");
            }

            _cts.Token.Register(() =>
            {
                _logger.LogDebug("### CAP background task is stopping.");


                foreach (var item in _processors)
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
            foreach (var item in _processors)
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
            if (_disposed)
            {
                return;
            }
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BootstrapAsync();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();

            await base.StopAsync(cancellationToken);
        }

        private void CheckRequirement()
        {
            var marker = _serviceProvider.GetService<CapMarkerService>();
            if (marker == null)
            {
                throw new InvalidOperationException(
                    "AddCap() must be added on the service collection.   eg: services.AddCap(...)");
            }

            var messageQueueMarker = _serviceProvider.GetService<CapMessageQueueMakerService>();
            if (messageQueueMarker == null)
            {
                throw new InvalidOperationException(
                  $"You must be config transport provider for CAP!" + Environment.NewLine +
                  $"==================================================================================" + Environment.NewLine +
                  $"========   eg: services.AddCap( options => {{ options.UseRabbitMQ(...) }}); ========" + Environment.NewLine +
                  $"==================================================================================");
            }

            var databaseMarker = _serviceProvider.GetService<CapStorageMarkerService>();
            if (databaseMarker == null)
            {
                throw new InvalidOperationException(
                 $"You must be config storage provider for CAP!" + Environment.NewLine +
                 $"===================================================================================" + Environment.NewLine +
                 $"========   eg: services.AddCap( options => {{ options.UseSqlServer(...) }}); ========" + Environment.NewLine +
                 $"===================================================================================");
            }
        }
    }
}
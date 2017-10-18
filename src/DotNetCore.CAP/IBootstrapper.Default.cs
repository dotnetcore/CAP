using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    /// <inheritdoc />
    /// <summary>
    /// Default implement of <see cref="T:DotNetCore.CAP.IBootstrapper" />.
    /// </summary>
    internal class DefaultBootstrapper : IBootstrapper
    {
        private readonly IApplicationLifetime _appLifetime;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationTokenRegistration _ctsRegistration;
        private readonly ILogger<DefaultBootstrapper> _logger;
        private Task _bootstrappingTask;

        public DefaultBootstrapper(
            ILogger<DefaultBootstrapper> logger,
            IStorage storage,
            IApplicationLifetime appLifetime,
            IEnumerable<IProcessingServer> processors)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            Storage = storage;
            Processors = processors;

            _cts = new CancellationTokenSource();
            _ctsRegistration = appLifetime.ApplicationStopping.Register(() =>
            {
                _cts.Cancel();
                try
                {
                    _bootstrappingTask?.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException ex)
                {
                    _logger.ExpectedOperationCanceledException(ex);
                }
            });
        }

        protected IStorage Storage { get; }

        protected IEnumerable<IProcessingServer> Processors { get; }

        public Task BootstrapAsync()
        {
            return _bootstrappingTask = BootstrapTaskAsync();
        }

        private async Task BootstrapTaskAsync()
        {
            await Storage.InitializeAsync(_cts.Token);

            if (_cts.IsCancellationRequested) return;

            await BootstrapCoreAsync();

            if (_cts.IsCancellationRequested) return;

            foreach (var item in Processors)
                try
                {
                    item.Start();
                }
                catch (Exception ex)
                {
                    _logger.ServerStartedError(ex);
                }

            _ctsRegistration.Dispose();
            _cts.Dispose();
        }

        public virtual Task BootstrapCoreAsync()
        {
            _appLifetime.ApplicationStopping.Register(() =>
            {
                foreach (var item in Processors)
                    item.Dispose();
            });
            return Task.CompletedTask;
        }
    }
}
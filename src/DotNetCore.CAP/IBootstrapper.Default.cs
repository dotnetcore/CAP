using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Default implement of <see cref="IBootstrapper"/>.
    /// </summary>
    public class DefaultBootstrapper : IBootstrapper
    {
        private IApplicationLifetime _appLifetime;
        private CancellationTokenSource _cts;
        private CancellationTokenRegistration _ctsRegistration;
        private Task _bootstrappingTask;

        public DefaultBootstrapper(
            IOptions<CapOptions> options,
            ICapMessageStore storage,
            IApplicationLifetime appLifetime,
            IServiceProvider provider)
        {
            Options = options.Value;
            Storage = storage;
            _appLifetime = appLifetime;
            Provider = provider;
            Servers = Provider.GetServices<IProcessingServer>();

            _cts = new CancellationTokenSource();
            _ctsRegistration = appLifetime.ApplicationStopping.Register(() =>
            {
                _cts.Cancel();
                try
                {
                    _bootstrappingTask?.Wait();
                }
                catch (OperationCanceledException)
                {
                }
            });
        }

        protected CapOptions Options { get; }

        protected ICapMessageStore Storage { get; }

        protected IEnumerable<IProcessingServer> Servers { get; }

        public IServiceProvider Provider { get; private set; }

        public Task BootstrapAsync()
        {
            return (_bootstrappingTask = BootstrapTaskAsync());
        }

        private async Task BootstrapTaskAsync()
        {
            if (_cts.IsCancellationRequested) return;

            if (_cts.IsCancellationRequested) return;

            await BootstrapCoreAsync();

            if (_cts.IsCancellationRequested) return;

            foreach (var item in Servers)
            {
                try
                {
                    item.Start();
                }
                catch (Exception)
                {
                }
            }

            _ctsRegistration.Dispose();
            _cts.Dispose();
        }

        public virtual Task BootstrapCoreAsync()
        {
            _appLifetime.ApplicationStopping.Register(() =>
            {
                foreach (var item in Servers)
                {
                    item.Dispose();
                }
            });
            return Task.FromResult(0);
        }
    }
}
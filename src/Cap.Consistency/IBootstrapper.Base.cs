using System;
using System.Threading;
using System.Threading.Tasks;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Store;
using Microsoft.AspNetCore.Hosting;

namespace Cap.Consistency
{
    public abstract class BootstrapperBase : IBootstrapper
    {
        private IApplicationLifetime _appLifetime;
        private CancellationTokenSource _cts;
        private CancellationTokenRegistration _ctsRegistration;
        private Task _bootstrappingTask;

        public BootstrapperBase(
            ConsistencyOptions options,
            ConsistencyMessageManager storage,
            ITopicServer server,
            IApplicationLifetime appLifetime,
            IServiceProvider provider) {
            Options = options;
            Storage = storage;
            Server = server;
            _appLifetime = appLifetime;
            Provider = provider;

            _cts = new CancellationTokenSource();
            _ctsRegistration = appLifetime.ApplicationStopping.Register(() => {
                _cts.Cancel();
                try {
                    _bootstrappingTask?.Wait();
                }
                catch (OperationCanceledException) {
                }
            });
        }

        protected ConsistencyOptions Options { get; }

        protected ConsistencyMessageManager Storage { get; }

        protected ITopicServer Server { get; }

        public IServiceProvider Provider { get; private set; }

        public Task BootstrapAsync() {
            return (_bootstrappingTask = BootstrapTaskAsync());
        }

        private async Task BootstrapTaskAsync() {
            if (_cts.IsCancellationRequested) return;

            if (_cts.IsCancellationRequested) return;

            await BootstrapCoreAsync();
            if (_cts.IsCancellationRequested) return;

            Server.Start();

            _ctsRegistration.Dispose();
            _cts.Dispose();
        }

        public virtual Task BootstrapCoreAsync() {
            _appLifetime.ApplicationStopping.Register(() => Server.Dispose());
            return Task.FromResult(0);
        }
    }
}
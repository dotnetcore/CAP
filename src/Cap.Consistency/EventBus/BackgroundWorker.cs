using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.EventBus
{
    public abstract class BackgroundWorker
          : IBackgroundWorker, IDisposable
    {
        protected readonly ILogger _logger;

#if FEATURE_THREAD
        protected Thread _dispatchThread;
#else
        protected Task _dispatchThread;
#endif
        protected CancellationTokenSource _cancellationTokenSource;

        public virtual bool IsRunning {
            get {
                return this._dispatchThread != null &&
#if FEATURE_THREAD
                    this._dispatchThread.ThreadState == ThreadState.Running;
#else
                    this._dispatchThread.Status == TaskStatus.Running;
#endif
            }
        }

        protected BackgroundWorker(ILoggerFactory loggerFactory) {
            this._logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public virtual void Start() {
            this.Start(false);
        }

        public virtual void Start(bool force) {
            if (!force) {
                if (this.IsRunning) {
                    return;
                }
            }
            this._cancellationTokenSource = new CancellationTokenSource();
#if !FEATURE_THREAD
            this._dispatchThread = this.ThreadWorker(this._cancellationTokenSource.Token);
#else
            this._dispatchThread = new Thread((userObject) =>
            {
                this.ThreadWorker(userObject).GetAwaiter().GetResult();
            })
            {
                IsBackground = true,
                Name = $"{this.GetType().Name}-Thread-{Guid.NewGuid().ToString()}"
            };
            this._dispatchThread.Start(this._cancellationTokenSource.Token);
#endif
        }

        public virtual void Stop(int timeout = 2000) {
            Task.WaitAny(Task.Run(() => {
                this._cancellationTokenSource.Cancel();
                while (this.IsRunning) {
                    Task.Delay(500).GetAwaiter().GetResult();
                }
            }), Task.Delay(timeout));
        }

        protected virtual async Task ThreadWorker(object userObject) {
            this._logger.LogInformation($"Background worker {this.GetType().FullName} has been started.");
            var token = (CancellationToken)userObject;
            while (!token.IsCancellationRequested && await this.Process()) {
            }
            this._logger.LogInformation($"Background worker {this.GetType().FullName} has been stopped.");
        }

        protected abstract Task<bool> Process();

        #region IDisposable

        // Flag: Has Dispose already been called?
        private bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                // Free any other managed objects here.
                this.Stop();
            }

            // Free any unmanaged objects here.
            disposed = true;
        }

        #endregion IDisposable
    }
}
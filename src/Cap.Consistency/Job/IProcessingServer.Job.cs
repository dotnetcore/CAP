using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cap.Consistency.Job;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Cap.Consistency.Infrastructure;
using Microsoft.Extensions.Options;

namespace Cap.Consistency
{
    public class JobProcessingServer : IProcessingServer, IDisposable
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;
        private IServiceProvider _provider;
        private CancellationTokenSource _cts;
        private IJobProcessor _processor;
        private ConsistencyOptions _options;
        private ProcessingContext _context;
        private DefaultCronJobRegistry _defaultJobRegistry;
        private Task _compositeTask;
        private bool _disposed;

        public JobProcessingServer(
            ILogger<JobProcessingServer> logger,
            ILoggerFactory loggerFactory,
            IServiceProvider provider,
            DefaultCronJobRegistry defaultJobRegistry,
            IOptions<ConsistencyOptions> options) {

            _logger = logger;
            _loggerFactory = loggerFactory;
            _provider = provider;
            _defaultJobRegistry = defaultJobRegistry;
            _options = options.Value;
            _cts = new CancellationTokenSource();
        }

        public void Start() {

            var processorCount = Environment.ProcessorCount;
            _processor = _provider.GetService<IJobProcessor>();
            _logger.ServerStarting(processorCount, 1);

            _context = new ProcessingContext(
                _provider,
                _defaultJobRegistry,
                _cts.Token);

            _compositeTask = Task.Run(() => {
                InfiniteRetry(_processor).ProcessAsync(_context);
            });
        }

        public void Dispose() {
            if (_disposed) {
                return;
            }
            _disposed = true;

            _logger.ServerShuttingDown();
            _cts.Cancel();
            try {
                _compositeTask.Wait((int)TimeSpan.FromSeconds(60).TotalMilliseconds);
            }
            catch (AggregateException ex) {
                var innerEx = ex.InnerExceptions[0];
                if (!(innerEx is OperationCanceledException)) {
                    _logger.ExpectedOperationCanceledException(innerEx);
                }
            }
        }

        private IJobProcessor InfiniteRetry(IJobProcessor inner) {
            return new InfiniteRetryProcessor(inner, _loggerFactory);
        }           

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class CapProcessingServer : IProcessingServer
    {
        private readonly CancellationTokenSource _cts;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CapOptions _options;
        private readonly IServiceProvider _provider;
        private Task _compositeTask;
        private ProcessingContext _context;
        private bool _disposed;

        private IProcessor[] _processors;

        public CapProcessingServer(
            ILogger<CapProcessingServer> logger,
            ILoggerFactory loggerFactory,
            IServiceProvider provider,
            IOptions<CapOptions> options)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _provider = provider;
            _options = options.Value;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            var processorCount = _options.QueueProcessorCount;
            _processors = GetProcessors(processorCount);

            _logger.ServerStarting(processorCount, _processors.Length);

            _context = new ProcessingContext(_provider, _cts.Token);

            var processorTasks = _processors
                .Select(InfiniteRetry)
                .Select(p => p.ProcessAsync(_context));
            _compositeTask = Task.WhenAll(processorTasks);
        }

        public void Pulse()
        {
            _logger.LogTrace("Pulsing the Queuer.");
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _logger.ServerShuttingDown();
            _cts.Cancel();
            try
            {
                _compositeTask.Wait((int) TimeSpan.FromSeconds(10).TotalMilliseconds);
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerExceptions[0];
                if (!(innerEx is OperationCanceledException))
                    _logger.ExpectedOperationCanceledException(innerEx);
            }
        }

        private IProcessor InfiniteRetry(IProcessor inner)
        {
            return new InfiniteRetryProcessor(inner, _loggerFactory);
        }

        private IProcessor[] GetProcessors(int processorCount)
        {
            var returnedProcessors = new List<IProcessor>
            {
                _provider.GetRequiredService<NeedRetryMessageProcessor>(),
                _provider.GetRequiredService<IAdditionalProcessor>()
            };

            return returnedProcessors.ToArray();
        }
    }
}
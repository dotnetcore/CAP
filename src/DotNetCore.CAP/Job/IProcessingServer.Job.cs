using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Job;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    public class JobProcessingServer : IProcessingServer, IDisposable
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;
        private IServiceProvider _provider;
        private IJobProcessor[] _processors;
        private CancellationTokenSource _cts;
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
            IOptions<ConsistencyOptions> options)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _provider = provider;
            _defaultJobRegistry = defaultJobRegistry;
            _options = options.Value;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            var processorCount = Environment.ProcessorCount;
            processorCount = 1;
            _processors = GetProcessors(processorCount);
            _logger.ServerStarting(processorCount, 1);

            _context = new ProcessingContext(
                _provider,
                _defaultJobRegistry,
                _cts.Token);

            var processorTasks = _processors
                .Select(p => InfiniteRetry(p))
                .Select(p => p.ProcessAsync(_context));
            _compositeTask = Task.WhenAll(processorTasks);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _logger.ServerShuttingDown();
            _cts.Cancel();
            try
            {
                _compositeTask.Wait((int)TimeSpan.FromSeconds(60).TotalMilliseconds);
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerExceptions[0];
                if (!(innerEx is OperationCanceledException))
                {
                    _logger.ExpectedOperationCanceledException(innerEx);
                }
            }
        }

        private IJobProcessor InfiniteRetry(IJobProcessor inner)
        {
            return new InfiniteRetryProcessor(inner, _loggerFactory);
        }

        private IJobProcessor[] GetProcessors(int processorCount)
        {
            var returnedProcessors = new List<IJobProcessor>();
            for (int i = 0; i < processorCount; i++)
            {
                var processors = _provider.GetServices<IJobProcessor>();
                foreach (var processor in processors)
                {
                    if (processor is CronJobProcessor)
                    {
                        if (i == 0)  // only add first cronJob
                            returnedProcessors.Add(processor);
                    }
                    else
                    {
                        returnedProcessors.Add(processor);
                    }
                }
            }

            return returnedProcessors.ToArray();
        }
    }
}
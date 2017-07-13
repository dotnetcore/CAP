using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Job
{
    public class JobProcessingServer : IProcessingServer, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _provider;
        private readonly CancellationTokenSource _cts;
        private readonly CapOptions _options;

        private IJobProcessor[] _processors;
        private IMessageJobProcessor[] _messageProcessors;
        private ProcessingContext _context;
        private Task _compositeTask;
        private bool _disposed;

        public JobProcessingServer(
            ILogger<JobProcessingServer> logger,
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
            var processorCount = Environment.ProcessorCount;
            processorCount = 1;
            _processors = GetProcessors(processorCount);
            _logger.ServerStarting(processorCount, processorCount);

            _context = new ProcessingContext(_provider, _cts.Token);

            var processorTasks = _processors
                .Select(p => InfiniteRetry(p))
                .Select(p => p.ProcessAsync(_context));
            _compositeTask = Task.WhenAll(processorTasks);
        }

        public void Pulse()
        {
            if (!AllProcessorsWaiting())
            {
                // Some processor is still executing jobs so no need to pulse.
                return;
            }

            _logger.LogTrace("Pulsing the JobQueuer.");

            WaitHandleEx.QueuePulseEvent.Set();
        }

        private bool AllProcessorsWaiting()
        {
            foreach (var processor in _messageProcessors)
            {
                if (!processor.Waiting)
                {
                    return false;
                }
            }
            return true;
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
                var messageProcessors = _provider.GetServices<IMessageJobProcessor>();
                _messageProcessors = messageProcessors.ToArray();
                returnedProcessors.AddRange(messageProcessors);
            }

            returnedProcessors.Add(_provider.GetService<JobQueuer>());
            //returnedProcessors.Add(_provider.GetService<IAdditionalProcessor>());

            return returnedProcessors.ToArray();
        }
    }
}
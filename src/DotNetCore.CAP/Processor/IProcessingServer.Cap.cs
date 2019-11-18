// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Processor
{
    public class CapProcessingServer : IProcessingServer
    {
        private readonly CancellationTokenSource _cts;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _provider;

        private Task _compositeTask;
        private ProcessingContext _context;
        private bool _disposed;

        public CapProcessingServer(
            ILogger<CapProcessingServer> logger,
            ILoggerFactory loggerFactory,
            IServiceProvider provider)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _provider = provider;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            _logger.ServerStarting();

            _context = new ProcessingContext(_provider, _cts.Token);

            var processorTasks = GetProcessors()
                .Select(InfiniteRetry)
                .Select(p => p.ProcessAsync(_context));
            _compositeTask = Task.WhenAll(processorTasks);
        }

        public void Pulse()
        {
            _logger.LogTrace("Pulsing the processor.");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _disposed = true;

                _logger.ServerShuttingDown();
                _cts.Cancel();

                _compositeTask?.Wait((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerExceptions[0];
                if (!(innerEx is OperationCanceledException))
                {
                    _logger.ExpectedOperationCanceledException(innerEx);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An exception was occured when disposing.");
            }
            finally
            {
                _logger.LogInformation("### CAP shutdown!");
            }
        }

        private IProcessor InfiniteRetry(IProcessor inner)
        {
            return new InfiniteRetryProcessor(inner, _loggerFactory);
        }

        private IProcessor[] GetProcessors()
        {
            var returnedProcessors = new List<IProcessor>
            {
                _provider.GetRequiredService<TransportCheckProcessor>(),
                _provider.GetRequiredService<MessageNeedToRetryProcessor>(),
                _provider.GetRequiredService<CollectorProcessor>()
            };

            return returnedProcessors.ToArray();
        }
    }
}
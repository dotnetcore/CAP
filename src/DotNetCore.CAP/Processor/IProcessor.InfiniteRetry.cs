// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Processor
{
    public class InfiniteRetryProcessor : IProcessor
    {
        private readonly IProcessor _inner;
        private readonly ILogger _logger;

        public InfiniteRetryProcessor(
            IProcessor inner,
            ILoggerFactory loggerFactory)
        {
            _inner = inner;
            _logger = loggerFactory.CreateLogger<InfiniteRetryProcessor>();
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            while (!context.IsStopping)
            {
                try
                {
                    await _inner.ProcessAsync(context);
                }
                catch (OperationCanceledException)
                {
                    //ignore
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Processor '{ProcessorName}' failed. Retrying...", _inner.ToString());
                    await context.WaitAsync(TimeSpan.FromSeconds(2));
                }
            }
        }

        public override string ToString()
        {
            return _inner.ToString();
        }
    }
}
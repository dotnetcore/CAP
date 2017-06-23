using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Job
{
    public class InfiniteRetryProcessor : IJobProcessor
    {
        private IJobProcessor _inner;
        private ILogger _logger;

        public InfiniteRetryProcessor(
            IJobProcessor inner,
            ILoggerFactory loggerFactory)
        {
            _inner = inner;
            _logger = loggerFactory.CreateLogger<InfiniteRetryProcessor>();
        }

        public override string ToString() => _inner.ToString();

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
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        1,
                        ex,
                        "Prcessor '{ProcessorName}' failed. Retrying...", _inner.ToString());
                }
            }
        }
    }
}
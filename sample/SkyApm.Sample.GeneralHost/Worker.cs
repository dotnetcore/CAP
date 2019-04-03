using Microsoft.Extensions.Hosting;
using SkyApm.Tracing;
using SkyApm.Tracing.Segments;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyApm.Sample.GeneralHost
{
    public class Worker : BackgroundService
    {
        private readonly ITracingContext _tracingContext;

        public Worker(ITracingContext tracingContext)
        {
            _tracingContext = tracingContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var context = _tracingContext.CreateEntrySegmentContext(nameof(ExecuteAsync), new TextCarrierHeaderCollection(new Dictionary<string, string>()));

                await Task.Delay(1000, stoppingToken);

                context.Span.AddLog(LogEvent.Message($"Worker running at: {DateTime.Now}"));

                _tracingContext.Release(context);
            }
        }
    }
}

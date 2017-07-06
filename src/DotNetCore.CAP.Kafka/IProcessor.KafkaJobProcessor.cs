using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Job;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    public class KafkaJobProcessor : IJobProcessor
    {
        private readonly KafkaOptions _kafkaOptions;
        private readonly CancellationTokenSource _cts;

        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        private readonly TimeSpan _pollingDelay;

        public KafkaJobProcessor(
            IOptions<CapOptions> capOptions,
            IOptions<KafkaOptions> kafkaOptions,
            ILogger<KafkaJobProcessor> logger,
            IServiceProvider provider)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions.Value;
            _provider = provider;
            _cts = new CancellationTokenSource();
            _pollingDelay = TimeSpan.FromSeconds(capOptions.Value.PollingDelay);
        }

        public bool Waiting { get; private set; }

        public Task ProcessAsync(ProcessingContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            context.ThrowIfStopping();
            return ProcessCoreAsync(context);
        }

        public async Task ProcessCoreAsync(ProcessingContext context)
        {
            try
            {
                var worked = await Step(context);

                context.ThrowIfStopping();

                Waiting = true;

                if (!worked)
                {
                    var token = GetTokenToWaitOn(context);
                }

                await WaitHandleEx.WaitAnyAsync(WaitHandleEx.PulseEvent,
                    context.CancellationToken.WaitHandle, _pollingDelay);
            }
            finally
            {
                Waiting = false;
            }
        }

        protected virtual CancellationToken GetTokenToWaitOn(ProcessingContext context)
        {
            return context.CancellationToken;
        }

        private async Task<bool> Step(ProcessingContext context)
        {
            using (var scopedContext = context.CreateScope())
            {
                var provider = scopedContext.Provider;
                var messageStore = provider.GetRequiredService<ICapMessageStore>();
                var message = await messageStore.GetNextSentMessageToBeEnqueuedAsync();
                if (message == null) return true;
                try
                {
                    var sp = Stopwatch.StartNew();
                    message.StatusName = StatusName.Processing;
                    await messageStore.UpdateSentMessageAsync(message);

                    await ExecuteJobAsync(message.KeyName, message.Content);

                    sp.Stop();

                    message.StatusName = StatusName.Succeeded;
                    await messageStore.UpdateSentMessageAsync(message);
                    _logger.JobExecuted(sp.Elapsed.TotalSeconds);
                }
                catch (Exception ex)
                {
                    _logger.ExceptionOccuredWhileExecutingJob(message.KeyName, ex);
                    return false;
                }
            }
            return true;
        }

        private Task ExecuteJobAsync(string topic, string content)
        {
            var config = _kafkaOptions.AsRdkafkaConfig();
            using (var producer = new Producer<Null, string>(config, null, new StringSerializer(Encoding.UTF8)))
            {
                producer.ProduceAsync(topic, null, content);
                producer.Flush();
            }
            return Task.CompletedTask;
        }
    }
}
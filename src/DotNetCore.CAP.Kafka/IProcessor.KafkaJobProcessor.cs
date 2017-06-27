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
        private readonly CapOptions _capOptions;
        private readonly KafkaOptions _kafkaOptions;
        private readonly CancellationTokenSource _cts;

        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        private TimeSpan _pollingDelay;

        public KafkaJobProcessor(
            IOptions<CapOptions> capOptions,
            IOptions<KafkaOptions> kafkaOptions,
            ILogger<KafkaJobProcessor> logger,
            IServiceProvider provider)
        {
            _logger = logger;
            _capOptions = capOptions.Value;
            _kafkaOptions = kafkaOptions.Value;
            _provider = provider;
            _cts = new CancellationTokenSource();
            _pollingDelay = TimeSpan.FromSeconds(_capOptions.PollingDelay);
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
                try
                {
                    var message = await messageStore.GetNextSentMessageToBeEnqueuedAsync();
                    if (message != null)
                    {
                        var sp = Stopwatch.StartNew();
                        message.StateName = StateName.Processing;
                        await messageStore.UpdateSentMessageAsync(message);

                        var jobResult = ExecuteJob(message.KeyName, message.Content);

                        sp.Stop();

                        if (!jobResult)
                        {
                            _logger.JobFailed(new Exception("topic send failed"));
                        }
                        else
                        {
                            //TODO ： the state will be deleted when release.
                            message.StateName = StateName.Succeeded;
                            await messageStore.UpdateSentMessageAsync(message);

                            _logger.JobExecuted(sp.Elapsed.TotalSeconds);
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        private bool ExecuteJob(string topic, string content)
        {
            try
            {
                var config = new Dictionary<string, object> { { "bootstrap.servers", _kafkaOptions.Host } };
                using (var producer = new Producer<Null, string>(config, null, new StringSerializer(Encoding.UTF8)))
                {
                    var message = producer.ProduceAsync(topic, null, content).Result;
                    if (message.Error.Code == ErrorCode.NoError)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ExceptionOccuredWhileExecutingJob(topic, ex);
                return false;
            }
        }
    }
}
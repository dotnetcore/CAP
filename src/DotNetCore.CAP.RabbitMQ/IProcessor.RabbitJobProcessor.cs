using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Job;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public class RabbitJobProcessor : IJobProcessor
    {
        private readonly CapOptions _capOptions;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly CancellationTokenSource _cts;

        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        private TimeSpan _pollingDelay;

        public RabbitJobProcessor(
            IOptions<CapOptions> capOptions,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            IOptions<RabbitMQOptions> options,
            ILogger<RabbitJobProcessor> logger,
            IServiceProvider provider)
        {
            _logger = logger;
            _capOptions = capOptions.Value;
            _rabbitMQOptions = rabbitMQOptions.Value;
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

                await WaitHandleEx.WaitAnyAsync(WaitHandleEx.PulseEvent, context.CancellationToken.WaitHandle, _pollingDelay);
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
                var factory = new ConnectionFactory()
                {
                    HostName = _rabbitMQOptions.HostName,
                    UserName = _rabbitMQOptions.UserName,
                    Port = _rabbitMQOptions.Port,
                    Password = _rabbitMQOptions.Password,
                    VirtualHost = _rabbitMQOptions.VirtualHost,
                    RequestedConnectionTimeout = _rabbitMQOptions.RequestedConnectionTimeout,
                    SocketReadTimeout = _rabbitMQOptions.SocketReadTimeout,
                    SocketWriteTimeout = _rabbitMQOptions.SocketWriteTimeout
                };

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: "topic_logs",
                                            type: "topic");

                    var body = Encoding.UTF8.GetBytes(content);
                    channel.BasicPublish(exchange: "topic_logs",
                                         routingKey: topic,
                                         basicProperties: null,
                                         body: body);

                    return true;
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
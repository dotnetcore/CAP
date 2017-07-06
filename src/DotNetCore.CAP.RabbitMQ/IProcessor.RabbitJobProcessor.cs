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
        private readonly RabbitMQOptions _rabbitMqOptions;
        private readonly CancellationTokenSource _cts;

        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        private readonly TimeSpan _pollingDelay;

        public RabbitJobProcessor(
            IOptions<CapOptions> capOptions,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            ILogger<RabbitJobProcessor> logger,
            IServiceProvider provider)
        {
            _logger = logger;
            _rabbitMqOptions = rabbitMQOptions.Value;
            _provider = provider;
            _cts = new CancellationTokenSource();
            
            var capOptions1 = capOptions.Value;
            _pollingDelay = TimeSpan.FromSeconds(capOptions1.PollingDelay);
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
                try
                {
                    if (message != null)
                    {
                        var sp = Stopwatch.StartNew();
                        message.StatusName = StatusName.Processing;
                        await messageStore.UpdateSentMessageAsync(message);

                        ExecuteJob(message.KeyName, message.Content);

                        sp.Stop();

                        message.StatusName = StatusName.Succeeded;
                        await messageStore.UpdateSentMessageAsync(message);

                        _logger.JobExecuted(sp.Elapsed.TotalSeconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ExceptionOccuredWhileExecutingJob(message?.KeyName, ex);
                    return false;
                }
            }
            return true;
        }

        private void ExecuteJob(string topic, string content)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _rabbitMqOptions.HostName,
                UserName = _rabbitMqOptions.UserName,
                Port = _rabbitMqOptions.Port,
                Password = _rabbitMqOptions.Password,
                VirtualHost = _rabbitMqOptions.VirtualHost,
                RequestedConnectionTimeout = _rabbitMqOptions.RequestedConnectionTimeout,
                SocketReadTimeout = _rabbitMqOptions.SocketReadTimeout,
                SocketWriteTimeout = _rabbitMqOptions.SocketWriteTimeout
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var body = Encoding.UTF8.GetBytes(content);

                channel.ExchangeDeclare(_rabbitMqOptions.TopicExchangeName, _rabbitMqOptions.EXCHANGE_TYPE);
                channel.BasicPublish(exchange: _rabbitMqOptions.TopicExchangeName,
                                     routingKey: topic,
                                     basicProperties: null,
                                     body: body);
            }
        }
    }
}
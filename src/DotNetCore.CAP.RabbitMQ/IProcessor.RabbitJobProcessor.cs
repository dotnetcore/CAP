using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Job;
using DotNetCore.CAP.Job.States;
using DotNetCore.CAP.Models;
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
        private readonly IStateChanger _stateChanger;
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        private readonly TimeSpan _pollingDelay;
        internal static readonly AutoResetEvent PulseEvent = new AutoResetEvent(true);

        public RabbitJobProcessor(
            IOptions<CapOptions> capOptions,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            ILogger<RabbitJobProcessor> logger,
            IStateChanger stateChanger,
            IServiceProvider provider)
        {
            _logger = logger;
            _rabbitMqOptions = rabbitMQOptions.Value;
            _provider = provider;
            _stateChanger = stateChanger;
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

                await WaitHandleEx.WaitAnyAsync(PulseEvent,
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
            var fetched = default(IFetchedMessage);
            using (var scopedContext = context.CreateScope())
            {
                var provider = scopedContext.Provider;
                var messageStore = provider.GetRequiredService<ICapMessageStore>();
                var connection = provider.GetRequiredService<IStorageConnection>();
                
                if ((fetched = await connection.FetchNextSentMessageAsync()) != null)
                {
                    using (fetched)
                    {
                        var message = await connection.GetSentMessageAsync(fetched.MessageId);
                        try
                        {
                            var sp = Stopwatch.StartNew();
                            await _stateChanger.ChangeStateAsync(message, new ProcessingState(), connection);

                            if (message.Retries > 0)
                            {
                                _logger.JobRetrying(message.Retries);
                            }
                            var result = ExecuteJob(message.KeyName, message.Content);
                            sp.Stop();

                            var newState = default(IState);
                            if (!result.Succeeded)
                            {
                                var shouldRetry = await UpdateJobForRetryAsync(message, connection);
                                if (shouldRetry)
                                {
                                    newState = new ScheduledState();
                                    _logger.JobFailedWillRetry(result.Exception);
                                }
                                else
                                {
                                    newState = new FailedState();
                                    _logger.JobFailed(result.Exception);
                                }
                            }
                            else
                            {
                                newState = new SucceededState();
                            }
                            await _stateChanger.ChangeStateAsync(message, newState, connection);

                            fetched.RemoveFromQueue();

                            if (result.Succeeded)
                            {
                                _logger.JobExecuted(sp.Elapsed.TotalSeconds);
                            }
                        }

                        catch (Exception ex)
                        {
                            _logger.ExceptionOccuredWhileExecutingJob(message?.KeyName, ex);
                            return false;
                        }

                    }
                } 
            }
            return fetched != null;
        }

        private async Task<bool> UpdateJobForRetryAsync(CapSentMessage message, IStorageConnection connection)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var now = DateTime.UtcNow;
            var retries = ++message.Retries;
            if (retries >= retryBehavior.RetryCount)
            {
                return false;
            }

            var due = message.Added.AddSeconds(retryBehavior.RetryIn(retries));
            message.LastRun = due;
            using (var transaction = connection.CreateTransaction())
            {
                transaction.UpdateMessage(message);
                await transaction.CommitAsync();
            }
            return true;
        }

        private OperateResult ExecuteJob(string topic, string content)
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

            try
            {
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
                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                return OperateResult.Failed(ex, new OperateError() { Code = ex.HResult.ToString(), Description = ex.Message });
            }

        }
    }
}
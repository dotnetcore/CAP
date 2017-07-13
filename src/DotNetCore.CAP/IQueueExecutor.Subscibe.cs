using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    public class SubscibeQueueExecutor : IQueueExecutor
    {
        private readonly IConsumerInvokerFactory _consumerInvokerFactory;
        private readonly IConsumerClientFactory _consumerClientFactory;
        private readonly IStateChanger _stateChanger;
        private readonly ILogger _logger;

        private readonly MethodMatcherCache _selector;
        //private readonly CapOptions _options;

        public SubscibeQueueExecutor(
            IStateChanger stateChanger,
            MethodMatcherCache selector,
            IConsumerInvokerFactory consumerInvokerFactory,
            IConsumerClientFactory consumerClientFactory,
            ILogger<BasePublishQueueExecutor> logger)
        {
            _selector = selector;
            _consumerInvokerFactory = consumerInvokerFactory;
            _consumerClientFactory = consumerClientFactory;
            _stateChanger = stateChanger;
            _logger = logger;
        }

        public async Task<OperateResult> ExecuteAsync(IStorageConnection connection, IFetchedMessage fetched)
        {
            using (fetched)
            {
                var message = await connection.GetReceivedMessageAsync(fetched.MessageId);
                try
                {
                    var sp = Stopwatch.StartNew();
                    await _stateChanger.ChangeStateAsync(message, new ProcessingState(), connection);

                    if (message.Retries > 0)
                    {
                        _logger.JobRetrying(message.Retries);
                    }
                    var result = await ExecuteSubscribeAsync(message);
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

                    return OperateResult.Success;
                }
                catch (SubscriberNotFoundException ex)
                {
                    _logger.LogError(ex.Message);
                    return OperateResult.Failed(ex);
                }
                catch (Exception ex)
                {
                    _logger.ExceptionOccuredWhileExecutingJob(message?.KeyName, ex);
                    return OperateResult.Failed(ex);
                }
            }
        }

        protected virtual async Task<OperateResult> ExecuteSubscribeAsync(CapReceivedMessage receivedMessage)
        {
            try
            {
                var executeDescriptorGroup = _selector.GetTopicExector(receivedMessage.KeyName);

                if (!executeDescriptorGroup.ContainsKey(receivedMessage.Group))
                {
                    throw new SubscriberNotFoundException(receivedMessage.KeyName + " has not been found.");
                }

                // If there are multiple consumers in the same group, we will take the first
                var executeDescriptor = executeDescriptorGroup[receivedMessage.Group][0];
                var consumerContext = new ConsumerContext(executeDescriptor, receivedMessage.ToMessageContext());

                await _consumerInvokerFactory.CreateInvoker(consumerContext).InvokeAsync();

                return OperateResult.Success;
            }
            catch (SubscriberNotFoundException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                _logger.ConsumerMethodExecutingFailed($"Group:{receivedMessage.Group}, Topic:{receivedMessage.KeyName}", ex);
                return OperateResult.Failed(ex);
            }
        }

        private async Task<bool> UpdateJobForRetryAsync(CapReceivedMessage message, IStorageConnection connection)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var now = DateTime.Now;
            var retries = ++message.Retries;
            if (retries >= retryBehavior.RetryCount)
            {
                return false;
            }

            var due = message.Added.AddSeconds(retryBehavior.RetryIn(retries));
            message.ExpiresAt = due;
            using (var transaction = connection.CreateTransaction())
            {
                transaction.UpdateMessage(message);
                await transaction.CommitAsync();
            }
            return true;
        }

    }
}

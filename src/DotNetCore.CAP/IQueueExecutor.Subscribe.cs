using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    public class SubscribeQueueExecutor : IQueueExecutor
    {
        private readonly ILogger _logger;
        private readonly CapOptions _options;
        private readonly IStateChanger _stateChanger;
        private readonly ISubscriberExecutor _subscriberExecutor;

        public SubscribeQueueExecutor(
            CapOptions options,
            IStateChanger stateChanger,
            ISubscriberExecutor subscriberExecutor,
            ILogger<SubscribeQueueExecutor> logger)
        {
            _options = options;
            _subscriberExecutor = subscriberExecutor;
            _stateChanger = stateChanger;
            _logger = logger;
        }

        public async Task<OperateResult> ExecuteAsync(IStorageConnection connection, IFetchedMessage fetched)
        {
            var message = await connection.GetReceivedMessageAsync(fetched.MessageId);

            if (message == null)
            {
                _logger.LogError($"Can not find mesage at cap received message table, message id:{fetched.MessageId} !!!");
                return OperateResult.Failed();
            }

            try
            {
                var sp = Stopwatch.StartNew();
                await _stateChanger.ChangeStateAsync(message, new ProcessingState(), connection);

                if (message.Retries > 0)
                    _logger.JobRetrying(message.Retries);

                var result = await _subscriberExecutor.ExecuteAsync(message);
                sp.Stop();

                var state = GetNewState(result, message);

                await _stateChanger.ChangeStateAsync(message, state, connection);

                fetched.RemoveFromQueue();

                if (result.Succeeded)
                    _logger.JobExecuted(sp.Elapsed.TotalSeconds);

                return OperateResult.Success;
            }
            catch (SubscriberNotFoundException ex)
            {
                _logger.LogError(ex.Message);

                AddErrorReasonToContent(message, ex);

                await _stateChanger.ChangeStateAsync(message, new FailedState(), connection);

                fetched.RemoveFromQueue();

                return OperateResult.Failed(ex);
            }
            catch (Exception ex)
            {
                _logger.ExceptionOccuredWhileExecuting(message.Name, ex);

                fetched.Requeue();

                return OperateResult.Failed(ex);
            }
        }

        private IState GetNewState(OperateResult result, CapReceivedMessage message)
        {
            IState newState;
            if (!result.Succeeded)
            {
                var shouldRetry = UpdateMessageForRetry(message);
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
                AddErrorReasonToContent(message, result.Exception);
            }
            else
            {
                newState = new SucceededState(_options.SucceedMessageExpiredAfter);
            }
            return newState;
        }

        private static bool UpdateMessageForRetry(CapReceivedMessage message)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var retries = ++message.Retries;
            if (retries >= retryBehavior.RetryCount)
                return false;

            var due = message.Added.AddSeconds(retryBehavior.RetryIn(retries));
            message.ExpiresAt = due;

            return true;
        }

        private static void AddErrorReasonToContent(CapReceivedMessage message, Exception exception)
        { 
            message.Content = Helper.AddExceptionProperty(message.Content, exception);
        }
    }
}
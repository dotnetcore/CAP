using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    public abstract class BasePublishMessageSender : IPublishMessageSender, IPublishExecutor
    {
        private readonly ILogger _logger;
        private readonly CapOptions _options;
        private readonly IStorageConnection _connection;
        private readonly IStateChanger _stateChanger;

        protected BasePublishMessageSender(
            ILogger logger,
            CapOptions options,
            IStorageConnection connection,
            IStateChanger stateChanger)
        {
            _options = options;
            _connection = connection;
            _stateChanger = stateChanger;
            _logger = logger;
        }

        public async Task<OperateResult> SendAsync(CapPublishedMessage message)
        {
            try
            {
                var sp = Stopwatch.StartNew();

                if (message.Retries > 0)
                    _logger.JobRetrying(message.Retries);
                var result = await PublishAsync(message.Name, message.Content);

                sp.Stop();

                IState newState;

                if (!result.Succeeded)
                {
                    var shouldRetry = UpdateMessageForRetryAsync(message);
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

                    message.Content = Helper.AddExceptionProperty(message.Content, result.Exception);
                }
                else
                {
                    newState = new SucceededState(_options.SucceedMessageExpiredAfter);
                }

                await _stateChanger.ChangeStateAsync(message, newState, _connection);

                if (result.Succeeded)
                    _logger.JobExecuted(sp.Elapsed.TotalSeconds);

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                _logger.ExceptionOccuredWhileExecuting(message?.Name, ex);
                return OperateResult.Failed(ex);
            }
        }

        public abstract Task<OperateResult> PublishAsync(string keyName, string content);

        private static bool UpdateMessageForRetryAsync(CapPublishedMessage message)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var retries = ++message.Retries;
            if (retries >= retryBehavior.RetryCount)
                return false;

            var due = message.Added.AddSeconds(retryBehavior.RetryIn(retries));
            message.ExpiresAt = due;

            return true;
        }
    }
}
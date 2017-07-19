using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    public abstract class BasePublishQueueExecutor : IQueueExecutor
    {
        private readonly IStateChanger _stateChanger;
        private readonly ILogger _logger;

        protected BasePublishQueueExecutor(IStateChanger stateChanger,
            ILogger<BasePublishQueueExecutor> logger)
        {
            _stateChanger = stateChanger;
            _logger = logger;
        }

        public abstract Task<OperateResult> PublishAsync(string keyName, string content);

        public async Task<OperateResult> ExecuteAsync(IStorageConnection connection, IFetchedMessage fetched)
        {
            var message = await connection.GetPublishedMessageAsync(fetched.MessageId);
            try
            {
                var sp = Stopwatch.StartNew();
                await _stateChanger.ChangeStateAsync(message, new ProcessingState(), connection);

                if (message.Retries > 0)
                {
                    _logger.JobRetrying(message.Retries);
                }
                var result = await PublishAsync(message.Name, message.Content);
                sp.Stop();

                var newState = default(IState);
                if (!result.Succeeded)
                {
                    var shouldRetry = await UpdateMessageForRetryAsync(message, connection);
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
            catch (Exception ex)
            {
                _logger.ExceptionOccuredWhileExecutingJob(message?.Name, ex);
                return OperateResult.Failed(ex);
            }
        }

        private async Task<bool> UpdateMessageForRetryAsync(CapPublishedMessage message, IStorageConnection connection)
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
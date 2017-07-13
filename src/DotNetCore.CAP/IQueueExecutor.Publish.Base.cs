using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    public abstract class BasePublishQueueExecutor : IQueueExecutor
    {
        private readonly IStateChanger _stateChanger;
        private readonly ILogger _logger;

        public BasePublishQueueExecutor(IStateChanger stateChanger,
            ILogger<BasePublishQueueExecutor> logger)
        {
            _stateChanger = stateChanger;
            _logger = logger;
        }

        public abstract Task<OperateResult> PublishAsync(string keyName, string content);

        public async Task<OperateResult> ExecuteAsync(IStorageConnection connection, IFetchedMessage fetched)
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
                    var result = await PublishAsync(message.KeyName, message.Content);
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

                catch (Exception ex)
                {
                    _logger.ExceptionOccuredWhileExecutingJob(message?.KeyName, ex);
                    return OperateResult.Failed(ex);
                }
            }
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

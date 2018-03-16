using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    internal class DefaultSubscriberExecutor : ISubscriberExecutor
    {
        private readonly ICallbackMessageSender _callbackMessageSender;
        private readonly ILogger _logger;
        private readonly CapOptions _options;

        private readonly MethodMatcherCache _selector;
        private readonly IStateChanger _stateChanger;
        private readonly IStorageConnection _connection;

        public DefaultSubscriberExecutor(
            ILogger<DefaultSubscriberExecutor> logger,
            CapOptions options,
            IConsumerInvokerFactory consumerInvokerFactory,
            ICallbackMessageSender callbackMessageSender,
            IStateChanger stateChanger,
            IStorageConnection connection,
            MethodMatcherCache selector)
        {
            _selector = selector;
            _callbackMessageSender = callbackMessageSender;
            _options = options;
            _stateChanger = stateChanger;
            _connection = connection;
            _logger = logger;

            Invoker = consumerInvokerFactory.CreateInvoker();
        }

        private IConsumerInvoker Invoker { get; }

        public async Task<OperateResult> ExecuteAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            try
            {
                var sp = Stopwatch.StartNew();

                if (message.Retries > 0)
                    _logger.JobRetrying(message.Retries);

                var result = await InvokeAsync(message);

                sp.Stop();

                var state = GetNewState(result, message);

                await ChangeState(message, state);

                if (result.Succeeded)
                    _logger.JobExecuted(sp.Elapsed.TotalSeconds);

                return OperateResult.Success;
            }
            catch (SubscriberNotFoundException ex)
            {
                _logger.LogError(ex.Message);

                AddErrorReasonToContent(message, ex);

                ++message.Retries; //issue: https://github.com/dotnetcore/CAP/issues/90

                await ChangeState(message, new FailedState());

                return OperateResult.Failed(ex);
            }
            catch (Exception ex)
            {
                _logger.ExceptionOccuredWhileExecuting(message.Name, ex);

                return OperateResult.Failed(ex);
            }
        }

        private async Task ChangeState(CapReceivedMessage message, IState state)
        {
            await _stateChanger.ChangeStateAsync(message, state, _connection);
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

        private async Task<OperateResult> InvokeAsync(CapReceivedMessage receivedMessage)
        {
            if (!_selector.TryGetTopicExector(receivedMessage.Name, receivedMessage.Group,
                out var executor))
            {
                var error = "message can not be found subscriber. Message:" + receivedMessage;
                error += "\r\n  see: https://github.com/dotnetcore/CAP/issues/63";
                throw new SubscriberNotFoundException(error);
            }

            var consumerContext = new ConsumerContext(executor, receivedMessage.ToMessageContext());
            try
            {
                var ret = await Invoker.InvokeAsync(consumerContext);

                try
                {
                    if (!string.IsNullOrEmpty(ret.CallbackName))
                        await _callbackMessageSender.SendAsync(ret.MessageId, ret.CallbackName, ret.Result);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        $"Group:{receivedMessage.Group}, Topic:{receivedMessage.Name} callback message store failed",
                        e);

                    return OperateResult.Failed(e);
                }

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                _logger.ConsumerMethodExecutingFailed($"Group:{receivedMessage.Group}, Topic:{receivedMessage.Name}",
                    ex);

                return OperateResult.Failed(ex);
            }
        }
    }
}
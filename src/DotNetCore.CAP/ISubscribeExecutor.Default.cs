// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    internal class DefaultSubscriberExecutor : ISubscriberExecutor
    {
        private readonly ICallbackMessageSender _callbackMessageSender;
        private readonly IStorageConnection _connection;
        private readonly ILogger _logger;
        private readonly IStateChanger _stateChanger;
        private readonly CapOptions _options;
        private readonly MethodMatcherCache _selector;

        // diagnostics listener
        // ReSharper disable once InconsistentNaming
        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        public DefaultSubscriberExecutor(
            ILogger<DefaultSubscriberExecutor> logger,
            IOptions<CapOptions> options,
            IConsumerInvokerFactory consumerInvokerFactory,
            ICallbackMessageSender callbackMessageSender,
            IStateChanger stateChanger,
            IStorageConnection connection,
            MethodMatcherCache selector)
        {
            _selector = selector;
            _callbackMessageSender = callbackMessageSender;
            _options = options.Value;
            _stateChanger = stateChanger;
            _connection = connection;
            _logger = logger;

            Invoker = consumerInvokerFactory.CreateInvoker();
        }

        private IConsumerInvoker Invoker { get; }

        public async Task<OperateResult> ExecuteAsync(CapReceivedMessage message, CancellationToken cancellationToken)
        {
            bool retry;
            OperateResult result;
            do
            {
                var executedResult = await ExecuteWithoutRetryAsync(message, cancellationToken);
                result = executedResult.Item2;
                if (result == OperateResult.Success)
                {
                    return result;
                }
                retry = executedResult.Item1;
            } while (retry);

            return result;
        }

        /// <summary>
        /// Execute message consumption once.
        /// </summary>
        /// <param name="message">the message received of <see cref="CapReceivedMessage"/></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Item1 is need still retry, Item2 is executed result.</returns>
        private async Task<(bool, OperateResult)> ExecuteWithoutRetryAsync(CapReceivedMessage message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var sp = Stopwatch.StartNew();

                await InvokeConsumerMethodAsync(message, cancellationToken);

                sp.Stop();

                await SetSuccessfulState(message);

                _logger.ConsumerExecuted(sp.Elapsed.TotalMilliseconds);

                return (false, OperateResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred while executing the subscription method. Topic:{message.Name}, Id:{message.Id}");

                return (await SetFailedState(message, ex), OperateResult.Failed(ex));
            }
        }

        private Task SetSuccessfulState(CapReceivedMessage message)
        {
            var succeededState = new SucceededState(_options.SucceedMessageExpiredAfter);
            return _stateChanger.ChangeStateAsync(message, succeededState, _connection);
        }

        private async Task<bool> SetFailedState(CapReceivedMessage message, Exception ex)
        {
            if (ex is SubscriberNotFoundException)
            {
                message.Retries = _options.FailedRetryCount; // not retry if SubscriberNotFoundException
            }

            AddErrorReasonToContent(message, ex);

            var needRetry = UpdateMessageForRetry(message);

            await _stateChanger.ChangeStateAsync(message, new FailedState(), _connection);

            return needRetry;
        }

        private bool UpdateMessageForRetry(CapReceivedMessage message)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var retries = ++message.Retries;
            message.ExpiresAt = message.Added.AddSeconds(retryBehavior.RetryIn(retries));

            var retryCount = Math.Min(_options.FailedRetryCount, retryBehavior.RetryCount);
            if (retries >= retryCount)
            {
                if (retries == _options.FailedRetryCount)
                {
                    try
                    {
                        _options.FailedThresholdCallback?.Invoke(MessageType.Subscribe, message.Name, message.Content);

                        _logger.ConsumerExecutedAfterThreshold(message.Id, _options.FailedRetryCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.ExecutedThresholdCallbackFailed(ex);
                    }
                }
                return false;
            }

            _logger.ConsumerExecutionRetrying(message.Id, retries);

            return true;
        }

        private static void AddErrorReasonToContent(CapReceivedMessage message, Exception exception)
        {
            message.Content = Helper.AddExceptionProperty(message.Content, exception);
        }

        private async Task InvokeConsumerMethodAsync(CapReceivedMessage receivedMessage, CancellationToken cancellationToken)
        {
            if (!_selector.TryGetTopicExecutor(receivedMessage.Name, receivedMessage.Group,
                out var executor))
            {
                var error = $"Message can not be found subscriber. {receivedMessage} \r\n see: https://github.com/dotnetcore/CAP/issues/63";
                throw new SubscriberNotFoundException(error);
            }

            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.Empty;

            var consumerContext = new ConsumerContext(executor, receivedMessage.ToMessageContext());

            try
            {
                operationId = s_diagnosticListener.WriteSubscriberInvokeBefore(consumerContext);

                var ret = await Invoker.InvokeAsync(consumerContext, cancellationToken);

                s_diagnosticListener.WriteSubscriberInvokeAfter(operationId, consumerContext, startTime,
                    stopwatch.Elapsed);

                if (!string.IsNullOrEmpty(ret.CallbackName))
                {
                    await _callbackMessageSender.SendAsync(ret.MessageId, ret.CallbackName, ret.Result);
                }
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (Exception ex)
            {
                s_diagnosticListener.WriteSubscriberInvokeError(operationId, consumerContext, ex, startTime, 
                    stopwatch.Elapsed, receivedMessage.Retries + 1);

                throw new SubscriberExecutionFailedException(ex.Message, ex);
            }
        }
    }
}
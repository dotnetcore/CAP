// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
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
            bool retry;
            OperateResult result;
            do
            {
                result = await ExecuteWithoutRetryAsync(message);
                if (result == OperateResult.Success)
                {
                    return result;
                }
                retry = UpdateMessageForRetry(message);
            } while (retry);

            return result;
        }

        private async Task<OperateResult> ExecuteWithoutRetryAsync(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                var sp = Stopwatch.StartNew();

                await InvokeConsumerMethodAsync(message);

                sp.Stop();

                await SetSuccessfulState(message);

                _logger.ConsumerExecuted(sp.Elapsed.TotalSeconds);

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred while executing the subscription method. Topic:{message.Name}, Id:{message.Id}");

                await SetFailedState(message, ex);

                return OperateResult.Failed(ex);
            }
        }

        private Task SetSuccessfulState(CapReceivedMessage message)
        {
            var succeededState = new SucceededState(_options.SucceedMessageExpiredAfter);

            return _stateChanger.ChangeStateAsync(message, succeededState, _connection);
        }

        private Task SetFailedState(CapReceivedMessage message, Exception ex)
        {
            if (ex is SubscriberNotFoundException)
            {
                message.Retries = _options.FailedRetryCount; // not retry if SubscriberNotFoundException
            }

            AddErrorReasonToContent(message, ex);

            return _stateChanger.ChangeStateAsync(message, new FailedState(), _connection);
        }

        private bool UpdateMessageForRetry(CapReceivedMessage message)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var retries = ++message.Retries;
            var retryCount = Math.Min(_options.FailedRetryCount, retryBehavior.RetryCount);
            if (retries >= retryCount)
            {
                return false;
            }
            _logger.ConsumerExecutionRetrying(message.Id, retries);
            var due = message.Added.AddSeconds(retryBehavior.RetryIn(retries));
            message.ExpiresAt = due;
            return true;
        }

        private static void AddErrorReasonToContent(CapReceivedMessage message, Exception exception)
        {
            message.Content = Helper.AddExceptionProperty(message.Content, exception);
        }

        private async Task InvokeConsumerMethodAsync(CapReceivedMessage receivedMessage)
        {
            if (!_selector.TryGetTopicExector(receivedMessage.Name, receivedMessage.Group,
                out var executor))
            {
                var error = $"message can not be found subscriber, Message:{receivedMessage},\r\n see: https://github.com/dotnetcore/CAP/issues/63";
                throw new SubscriberNotFoundException(error);
            }

            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.Empty;

            var consumerContext = new ConsumerContext(executor, receivedMessage.ToMessageContext());

            try
            {
                operationId = s_diagnosticListener.WriteSubscriberInvokeBefore(consumerContext);

                var ret = await Invoker.InvokeAsync(consumerContext);

                s_diagnosticListener.WriteSubscriberInvokeAfter(operationId, consumerContext, startTime, stopwatch.Elapsed);

                if (!string.IsNullOrEmpty(ret.CallbackName))
                {
                    await _callbackMessageSender.SendAsync(ret.MessageId, ret.CallbackName, ret.Result);
                }
            }
            catch (Exception ex)
            {
                s_diagnosticListener.WriteSubscriberInvokeError(operationId, consumerContext, ex, startTime, stopwatch.Elapsed);

                throw new SubscriberExecutionFailedException(ex.Message, ex);
            }
        }
    }
}
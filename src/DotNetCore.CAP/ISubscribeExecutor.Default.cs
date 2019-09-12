// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    internal class DefaultSubscriberExecutor : ISubscriberExecutor
    {
        private readonly ICapPublisher _sender;
        private readonly IDataStorage _dataStorage;
        private readonly ILogger _logger;
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
            ICapPublisher sender,
            IDataStorage dataStorage,
            MethodMatcherCache selector)
        {
            _selector = selector;
            _sender = sender;
            _options = options.Value;
            _dataStorage = dataStorage;
            _logger = logger;

            Invoker = consumerInvokerFactory.CreateInvoker();
        }

        private IConsumerInvoker Invoker { get; }

        public async Task<OperateResult> ExecuteAsync(MediumMessage message, CancellationToken cancellationToken)
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

        private async Task<(bool, OperateResult)> ExecuteWithoutRetryAsync(MediumMessage message, CancellationToken cancellationToken)
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
                _logger.LogError(ex, $"An exception occurred while executing the subscription method. Topic:{message.Origin.GetName()}, Id:{message.DbId}");

                return (await SetFailedState(message, ex), OperateResult.Failed(ex));
            }
        }

        private Task SetSuccessfulState(MediumMessage message)
        {
            message.ExpiresAt = DateTime.Now.AddSeconds(_options.SucceedMessageExpiredAfter);
            return _dataStorage.ChangeReceiveStateAsync(message, StatusName.Succeeded);
        }

        private async Task<bool> SetFailedState(MediumMessage message, Exception ex)
        {
            if (ex is SubscriberNotFoundException)
            {
                message.Retries = _options.FailedRetryCount; // not retry if SubscriberNotFoundException
            }

            //TODO: Add exception to content
            // AddErrorReasonToContent(message, ex);

            var needRetry = UpdateMessageForRetry(message);

            await _dataStorage.ChangeReceiveStateAsync(message, StatusName.Failed);

            return needRetry;
        }

        private bool UpdateMessageForRetry(MediumMessage message)
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
                        _options.FailedThresholdCallback?.Invoke(MessageType.Subscribe, message.Origin);

                        _logger.ConsumerExecutedAfterThreshold(message.DbId, _options.FailedRetryCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.ExecutedThresholdCallbackFailed(ex);
                    }
                }
                return false;
            }

            _logger.ConsumerExecutionRetrying(message.DbId, retries);

            return true;
        }

        //private static void AddErrorReasonToContent(CapReceivedMessage message, Exception exception)
        //{
        //    message.Content = Helper.AddExceptionProperty(message.Content, exception);
        //}

        private async Task InvokeConsumerMethodAsync(MediumMessage message, CancellationToken cancellationToken)
        {
            if (!_selector.TryGetTopicExecutor(
                message.Origin.GetName(),
                message.Origin.GetGroup(),
                out var executor))
            {
                var error = $"Message can not be found subscriber. {message} \r\n see: https://github.com/dotnetcore/CAP/issues/63";
                throw new SubscriberNotFoundException(error);
            }

            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.Empty;

            var consumerContext = new ConsumerContext(executor, message.Origin);

            try
            {
                // operationId = s_diagnosticListener.WriteSubscriberInvokeBefore(consumerContext);

                var ret = await Invoker.InvokeAsync(consumerContext, cancellationToken);

                // s_diagnosticListener.WriteSubscriberInvokeAfter(operationId, consumerContext, startTime,stopwatch.Elapsed);

                if (!string.IsNullOrEmpty(ret.CallbackName))
                {
                    var header = new Dictionary<string, string>()
                    {
                        [Headers.CorrelationId] = message.Origin.GetId(),
                        [Headers.CorrelationSequence] = (message.Origin.GetCorrelationSequence() + 1).ToString()
                    };

                    await _sender.PublishAsync(ret.CallbackName, ret.Result, header, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (Exception ex)
            {
                // s_diagnosticListener.WriteSubscriberInvokeError(operationId, consumerContext, ex, startTime, stopwatch.Elapsed);

                throw new SubscriberExecutionFailedException(ex.Message, ex);
            }
        }
    }
}
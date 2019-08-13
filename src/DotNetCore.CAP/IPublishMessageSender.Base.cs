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
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP
{
    public abstract class BasePublishMessageSender : IPublishMessageSender, IPublishExecutor
    {
        private readonly IStorageConnection _connection;
        private readonly ILogger _logger;
        private readonly CapOptions _options;
        private readonly IStateChanger _stateChanger;

        protected abstract string ServersAddress { get; }

        // diagnostics listener
        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        protected BasePublishMessageSender(
            ILogger logger,
            IOptions<CapOptions> options,
            IStorageConnection connection,
            IStateChanger stateChanger)
        {
            _options = options.Value;
            _connection = connection;
            _stateChanger = stateChanger;
            _logger = logger;
        }

        public abstract Task<OperateResult> PublishAsync(string keyName, string content);

        public async Task<OperateResult> SendAsync(CapPublishedMessage message)
        {
            bool retry;
            OperateResult result;
            do
            {
                var executedResult = await SendWithoutRetryAsync(message);
                result = executedResult.Item2;
                if (result == OperateResult.Success)
                {
                    return result;
                }
                retry = executedResult.Item1;
            } while (retry);

            return result;
        }

        private async Task<(bool, OperateResult)> SendWithoutRetryAsync(CapPublishedMessage message)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            var tracingResult = TracingBefore(message.Name, message.Content);
            var operationId = tracingResult.Item1;

            var sendValues = tracingResult.Item2 != null
                ? Helper.AddTracingHeaderProperty(message.Content, tracingResult.Item2)
                : message.Content;

            var result = await PublishAsync(message.Name, sendValues);

            stopwatch.Stop();
            if (result.Succeeded)
            {
                await SetSuccessfulState(message);

                TracingAfter(operationId, message.Name, sendValues, startTime, stopwatch.Elapsed);

                return (false, OperateResult.Success);
            }
            else
            {
                TracingError(operationId, message, result, startTime, stopwatch.Elapsed);

                var needRetry = await SetFailedState(message, result.Exception);
                return (needRetry, OperateResult.Failed(result.Exception));
            }
        }


        private Task SetSuccessfulState(CapPublishedMessage message)
        {
            var succeededState = new SucceededState(_options.SucceedMessageExpiredAfter);
            return _stateChanger.ChangeStateAsync(message, succeededState, _connection);
        }

        private async Task<bool> SetFailedState(CapPublishedMessage message, Exception ex)
        {
            AddErrorReasonToContent(message, ex);

            var needRetry = UpdateMessageForRetry(message);

            await _stateChanger.ChangeStateAsync(message, new FailedState(), _connection);

            return needRetry;
        }

        private static void AddErrorReasonToContent(CapPublishedMessage message, Exception exception)
        {
            message.Content = Helper.AddExceptionProperty(message.Content, exception);
        }

        private bool UpdateMessageForRetry(CapPublishedMessage message)
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

                        _logger.SenderAfterThreshold(message.Id, _options.FailedRetryCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.ExecutedThresholdCallbackFailed(ex);
                    }
                }
                return false;
            }

            _logger.SenderRetrying(message.Id, retries);

            return true;
        }

        private (Guid, TracingHeaders) TracingBefore(string topic, string values)
        {
            Guid operationId = Guid.NewGuid();

            var eventData = new BrokerPublishEventData(
                operationId, "",
                ServersAddress, topic,
                values,
                DateTimeOffset.UtcNow);

            s_diagnosticListener.WritePublishBefore(eventData);

            return (operationId, eventData.Headers);  //if not enabled diagnostics ,the header will be null
        }

        private void TracingAfter(Guid operationId, string topic, string values, DateTimeOffset startTime, TimeSpan du)
        {
            var eventData = new BrokerPublishEndEventData(
                operationId,
                "",
                ServersAddress,
                topic,
                values,
                startTime,
                du);

            s_diagnosticListener.WritePublishAfter(eventData);
        }

        private void TracingError(Guid operationId, CapPublishedMessage message, OperateResult result, DateTimeOffset startTime, TimeSpan du)
        {
            var ex = new PublisherSentFailedException(result.ToString(), result.Exception);

            _logger.MessagePublishException(message.Id, result.ToString(), ex);

            var eventData = new BrokerPublishErrorEventData(
                operationId,
                "",
                ServersAddress,
                message.Name,
                message.Content,
                ex,
                startTime,
                du,
                message.Retries + 1);

            s_diagnosticListener.WritePublishError(eventData);
        }
    }
}
﻿// Copyright (c) .NET Core Community. All rights reserved.
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
    public abstract class BasePublishMessageSender : IPublishMessageSender, IPublishExecutor
    {
        private readonly IStorageConnection _connection;
        private readonly ILogger _logger;
        private readonly CapOptions _options;
        private readonly IStateChanger _stateChanger;

        protected string ServersAddress { get; set; }

        // diagnostics listener
        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

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

        public abstract Task<OperateResult> PublishAsync(string keyName, string content);

        public async Task<OperateResult> SendAsync(CapPublishedMessage message)
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

                return OperateResult.Success;
            }
            else
            {
                TracingError(operationId, message, result, startTime, stopwatch.Elapsed);

                await SetFailedState(message, result.Exception, out bool stillRetry);

                if (stillRetry)
                {
                    _logger.SenderRetrying(message.Id, message.Retries);

                    return await SendAsync(message);
                }
                else
                {
                    _logger.LogError($"The message still sent failed after {_options.FailedRetryCount} retries. We will stop retrying the message. " +
                                     "MessageId:" + message.Id);

                    try
                    {
                        _options.FailedThresholdCallback?.Invoke(MessageType.Publish, message.Name, message.Content);
                    }
                    catch (Exception ex_FailedCallback)
                    {
                        _logger.LogWarning("Failed call-back method raised an exception:" + ex_FailedCallback.Message);
                    }
                    return OperateResult.Failed(result.Exception);
                }               
            }
        }

        private static bool UpdateMessageForRetryAsync(CapPublishedMessage message)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var retries = ++message.Retries;
            if (retries >= retryBehavior.RetryCount)
            {
                return false;
            }

            var due = message.Added.AddSeconds(retryBehavior.RetryIn(retries));
            message.ExpiresAt = due;

            return true;
        }

        private Task SetSuccessfulState(CapPublishedMessage message)
        {
            var succeededState = new SucceededState(_options.SucceedMessageExpiredAfter);

            return _stateChanger.ChangeStateAsync(message, succeededState, _connection);
        }

        private Task SetFailedState(CapPublishedMessage message, Exception ex, out bool stillRetry)
        {
            IState newState = new FailedState();
            stillRetry = UpdateMessageForRetryAsync(message);
            if (stillRetry)
            {
                _logger.ConsumerExecutionFailedWillRetry(ex);
                return Task.CompletedTask;
            }

            AddErrorReasonToContent(message, ex);

            return _stateChanger.ChangeStateAsync(message, newState, _connection);
        }

        private static void AddErrorReasonToContent(CapPublishedMessage message, Exception exception)
        {
            message.Content = Helper.AddExceptionProperty(message.Content, exception);
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

            _logger.MessageHasBeenSent(du.TotalSeconds);
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
                du);

            s_diagnosticListener.WritePublishError(eventData);
        }
    }
}
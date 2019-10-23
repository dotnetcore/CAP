// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Serialization;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Internal
{
    internal class MessageSender : IMessageSender
    {
        private readonly IDataStorage _dataStorage;
        private readonly ISerializer _serializer;
        private readonly ITransport _transport;
        private readonly ILogger _logger;
        private readonly IOptions<CapOptions> _options;

        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        public MessageSender(
            ILogger<MessageSender> logger,
            IOptions<CapOptions> options,
            IDataStorage dataStorage,
            ISerializer serializer,
            ITransport transport)
        {
            _options = options;
            _dataStorage = dataStorage;
            _serializer = serializer;
            _transport = transport;
            _logger = logger;
        }

        public async Task<OperateResult> SendAsync(MediumMessage message)
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

        private async Task<(bool, OperateResult)> SendWithoutRetryAsync(MediumMessage message)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            var operationId = TracingBefore(message.Origin);

            var transportMsg = await _serializer.SerializeAsync(message.Origin);
            var result = await _transport.SendAsync(transportMsg);

            stopwatch.Stop();
            if (result.Succeeded)
            {
                await SetSuccessfulState(message);

                if (operationId != null)
                {
                    TracingAfter(operationId.Value, message.Origin, startTime, stopwatch.Elapsed);
                }

                return (false, OperateResult.Success);
            }
            else
            {
                if (operationId != null)
                {
                    TracingError(operationId.Value, message.Origin, result, startTime, stopwatch.Elapsed);
                }

                var needRetry = await SetFailedState(message, result.Exception);

                return (needRetry, OperateResult.Failed(result.Exception));
            }
        }

        private async Task SetSuccessfulState(MediumMessage message)
        {
            message.ExpiresAt = DateTime.Now.AddSeconds(_options.Value.SucceedMessageExpiredAfter);
            await _dataStorage.ChangePublishStateAsync(message, StatusName.Succeeded);
        }

        private async Task<bool> SetFailedState(MediumMessage message, Exception ex)
        {
            //TODO: Add exception to content

            var needRetry = UpdateMessageForRetry(message);

            await _dataStorage.ChangePublishStateAsync(message, StatusName.Failed);

            return needRetry;
        }

        private bool UpdateMessageForRetry(MediumMessage message)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var retries = ++message.Retries;
            message.ExpiresAt = message.Added.AddSeconds(retryBehavior.RetryIn(retries));

            var retryCount = Math.Min(_options.Value.FailedRetryCount, retryBehavior.RetryCount);
            if (retries >= retryCount)
            {
                if (retries == _options.Value.FailedRetryCount)
                {
                    try
                    {
                        _options.Value.FailedThresholdCallback?.Invoke(MessageType.Publish, message.Origin);

                        _logger.SenderAfterThreshold(message.DbId, _options.Value.FailedRetryCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.ExecutedThresholdCallbackFailed(ex);
                    }
                }
                return false;
            }

            _logger.SenderRetrying(message.DbId, retries);

            return true;
        }

        private Guid? TracingBefore(Message message)
        {
            if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerExtensions.CapBeforePublish))
            {
                var operationId = Guid.NewGuid();

                var eventData = new BrokerPublishEventData(operationId, "",_transport.Address, message,DateTimeOffset.UtcNow);

                s_diagnosticListener.Write(CapDiagnosticListenerExtensions.CapBeforePublish, eventData);

                return operationId;
            }

            return null;
        }

        private void TracingAfter(Guid operationId, Message message, DateTimeOffset startTime, TimeSpan du)
        {
            if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerExtensions.CapAfterPublish))
            {
                var eventData = new BrokerPublishEndEventData(operationId, "", _transport.Address, message, startTime, du);

                s_diagnosticListener.Write(CapDiagnosticListenerExtensions.CapAfterPublish, eventData);
            }
        }

        private void TracingError(Guid operationId, Message message, OperateResult result, DateTimeOffset startTime, TimeSpan du)
        {
            if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerExtensions.CapAfterPublish))
            {
                var ex = new PublisherSentFailedException(result.ToString(), result.Exception);
                var eventData = new BrokerPublishErrorEventData(operationId, "", _transport.Address, 
                    message, ex, startTime, du);

                s_diagnosticListener.Write(CapDiagnosticListenerExtensions.CapErrorPublish, eventData);
            }
        }
    }
}
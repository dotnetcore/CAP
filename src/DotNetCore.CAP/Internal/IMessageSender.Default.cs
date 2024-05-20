﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Internal;

internal class MessageSender : IMessageSender
{
    // ReSharper disable once InconsistentNaming
    protected static readonly DiagnosticListener s_diagnosticListener =
        new(CapDiagnosticListenerNames.DiagnosticListenerName);

    private readonly IDataStorage _dataStorage;
    private readonly ILogger _logger;
    private readonly IOptions<CapOptions> _options;
    private readonly ISerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITransport _transport;

    public MessageSender(
        ILogger<MessageSender> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _options = serviceProvider.GetRequiredService<IOptions<CapOptions>>();
        _dataStorage = serviceProvider.GetRequiredService<IDataStorage>();
        _serializer = serviceProvider.GetRequiredService<ISerializer>();
        _transport = serviceProvider.GetRequiredService<ITransport>();
    }

    public async Task<OperateResult> SendAsync(MediumMessage message)
    {
        bool retry;
        OperateResult result;
        do
        {
            (retry, result) = await SendWithoutRetryAsync(message).ConfigureAwait(false);
            if (result.Equals(OperateResult.Success)) return result;
        } while (retry);

        return result;
    }

    private async Task<(bool, OperateResult)> SendWithoutRetryAsync(MediumMessage message)
    {
        var transportMsg = await _serializer.SerializeAsync(message.Origin).ConfigureAwait(false);

        var tracingTimestamp = TracingBefore(transportMsg, _transport.BrokerAddress);

        var result = await _transport.SendAsync(transportMsg).ConfigureAwait(false);

        if (result.Succeeded)
        {
            await SetSuccessfulState(message).ConfigureAwait(false);

            TracingAfter(tracingTimestamp, transportMsg, _transport.BrokerAddress);

            return (false, OperateResult.Success);
        }

        TracingError(tracingTimestamp, transportMsg, _transport.BrokerAddress, result);

        var needRetry = await SetFailedState(message, result.Exception!).ConfigureAwait(false);

        return (needRetry, OperateResult.Failed(result.Exception!));
    }

    private async Task SetSuccessfulState(MediumMessage message)
    {
        message.ExpiresAt = DateTime.Now.AddSeconds(_options.Value.SucceedMessageExpiredAfter);
        await _dataStorage.ChangePublishStateAsync(message, StatusName.Succeeded).ConfigureAwait(false);
    }

    private async Task<bool> SetFailedState(MediumMessage message, Exception ex)
    {
        var needRetry = UpdateMessageForRetry(message);

        message.Origin.AddOrUpdateException(ex);
        message.ExpiresAt = message.Added.AddSeconds(_options.Value.FailedMessageExpiredAfter);

        await _dataStorage.ChangePublishStateAsync(message, StatusName.Failed).ConfigureAwait(false);

        return needRetry;
    }

    private bool UpdateMessageForRetry(MediumMessage message)
    {
        var retries = ++message.Retries;
        var retryCount = Math.Min(_options.Value.FailedRetryCount, 3);
        if (retries >= retryCount)
        {
            if (retries == _options.Value.FailedRetryCount)
                try
                {
                    _options.Value.FailedThresholdCallback?.Invoke(new FailedInfo
                    {
                        ServiceProvider = _serviceProvider,
                        MessageType = MessageType.Publish,
                        Message = message.Origin
                    });

                    _logger.SenderAfterThreshold(message.DbId, _options.Value.FailedRetryCount);
                }
                catch (Exception ex)
                {
                    _logger.ExecutedThresholdCallbackFailed(ex);
                }

            return false;
        }

        _logger.SenderRetrying(message.DbId, retries);

        return true;
    }

    #region tracing

    private long? TracingBefore(TransportMessage message, BrokerAddress broker)
    {
        CapEventCounterSource.Log.WritePublishMetrics();

        if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.BeforePublish))
        {
            var eventData = new CapEventDataPubSend
            {
                OperationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = message.GetName(),
                BrokerAddress = broker,
                TransportMessage = message
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.BeforePublish, eventData);

            return eventData.OperationTimestamp;
        }

        return null;
    }

    private void TracingAfter(long? tracingTimestamp, TransportMessage message, BrokerAddress broker)
    {
        if (tracingTimestamp != null && s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.AfterPublish))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var eventData = new CapEventDataPubSend
            {
                OperationTimestamp = now,
                Operation = message.GetName(),
                BrokerAddress = broker,
                TransportMessage = message,
                ElapsedTimeMs = now - tracingTimestamp.Value
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.AfterPublish, eventData);
        }
    }

    private void TracingError(long? tracingTimestamp, TransportMessage message, BrokerAddress broker,
        OperateResult result)
    {
        if (tracingTimestamp != null && s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.ErrorPublish))
        {
            var ex = new PublisherSentFailedException(result.ToString(), result.Exception);
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var eventData = new CapEventDataPubSend
            {
                OperationTimestamp = now,
                Operation = message.GetName(),
                BrokerAddress = broker,
                TransportMessage = message,
                ElapsedTimeMs = now - tracingTimestamp.Value,
                Exception = ex
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.ErrorPublish, eventData);
        }
    }

    #endregion
}
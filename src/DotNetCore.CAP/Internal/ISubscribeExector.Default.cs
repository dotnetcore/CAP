// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Internal;

internal class SubscribeExecutor : ISubscribeExecutor
{
    // diagnostics listener
    // ReSharper disable once InconsistentNaming
    private static readonly DiagnosticListener s_diagnosticListener =
        new(CapDiagnosticListenerNames.DiagnosticListenerName);

    private readonly IDataStorage _dataStorage;
    private readonly string? _hostName;
    private readonly ILogger _logger;
    private readonly CapOptions _options;
    private readonly IServiceProvider _provider;

    public SubscribeExecutor(
        ILogger<SubscribeExecutor> logger,
        IOptions<CapOptions> options,
        IServiceProvider provider)
    {
        _provider = provider;
        _logger = logger;
        _options = options.Value;

        _dataStorage = _provider.GetRequiredService<IDataStorage>();
        Invoker = _provider.GetRequiredService<ISubscribeInvoker>();
        _hostName = Helper.GetInstanceHostname();
    }

    private ISubscribeInvoker Invoker { get; }

    public async Task<OperateResult> ExecuteAsync(MediumMessage message, ConsumerExecutorDescriptor? descriptor = null, CancellationToken cancellationToken = default)
    {
        if (descriptor == null)
        {
            var selector = _provider.GetRequiredService<MethodMatcherCache>();
            if (!selector.TryGetTopicExecutor(message.Origin.GetName(), message.Origin.GetGroup()!, out descriptor))
            {
                var error =
                    $"Message (Name:{message.Origin.GetName()},Group:{message.Origin.GetGroup()}) can not be found subscriber." +
                    $"{Environment.NewLine} see: https://github.com/dotnetcore/CAP/issues/63";
                _logger.LogError(error);

                TracingError(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), message.Origin, null, new Exception(error));

                return OperateResult.Failed(new SubscriberNotFoundException(error));
            }
        }

        bool retry;
        OperateResult result;

        //record instance id
        message.Origin.Headers[Headers.ExecutionInstanceId] = _hostName;

        do
        {
            var (shouldRetry, operateResult) = await ExecuteWithoutRetryAsync(message, descriptor, cancellationToken).ConfigureAwait(false);
            result = operateResult;
            if (result.Equals(OperateResult.Success)) return result;
            retry = shouldRetry;
        } while (retry);

        return result;
    }

    private async Task<(bool, OperateResult)> ExecuteWithoutRetryAsync(MediumMessage message,
        ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.ConsumerExecuting(descriptor.ImplTypeInfo.Name, descriptor.MethodInfo.Name,
                descriptor.Attribute.Group);

            var sp = Stopwatch.StartNew();

            await InvokeConsumerMethodAsync(message, descriptor, cancellationToken).ConfigureAwait(false);

            sp.Stop();

            await SetSuccessfulState(message).ConfigureAwait(false);

            CapEventCounterSource.Log.WriteInvokeTimeMetrics(sp.Elapsed.TotalMilliseconds);
            _logger.ConsumerExecuted(descriptor.ImplTypeInfo.Name, descriptor.MethodInfo.Name,
                descriptor.Attribute.Group, sp.Elapsed.TotalMilliseconds, message.Origin.GetExecutionInstanceId());

            return (false, OperateResult.Success);
        }
        catch (Exception ex)
        {
            _logger.ConsumerExecuteFailed(message.Origin.GetName(), message.DbId,
                message.Origin.GetExecutionInstanceId(), ex);

            return (await SetFailedState(message, ex).ConfigureAwait(false), OperateResult.Failed(ex));
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
            message.Retries = _options.FailedRetryCount; // not retry if SubscriberNotFoundException

        var needRetry = UpdateMessageForRetry(message);

        message.Origin.AddOrUpdateException(ex);
        message.ExpiresAt = message.Added.AddSeconds(_options.FailedMessageExpiredAfter);

        await _dataStorage.ChangeReceiveStateAsync(message, StatusName.Failed).ConfigureAwait(false);

        return needRetry;
    }

    private bool UpdateMessageForRetry(MediumMessage message)
    {
        var retries = ++message.Retries;

        var retryCount = Math.Min(_options.FailedRetryCount, 3);
        if (retries >= retryCount)
        {
            if (retries == _options.FailedRetryCount)
                try
                {
                    _options.FailedThresholdCallback?.Invoke(new FailedInfo
                    {
                        ServiceProvider = _provider,
                        MessageType = MessageType.Subscribe,
                        Message = message.Origin
                    });

                    _logger.ConsumerExecutedAfterThreshold(message.DbId, _options.FailedRetryCount);
                }
                catch (Exception ex)
                {
                    _logger.ExecutedThresholdCallbackFailed(ex);
                }

            return false;
        }

        _logger.ConsumerExecutionRetrying(message.DbId, retries);

        return true;
    }

    private async Task InvokeConsumerMethodAsync(MediumMessage message, ConsumerExecutorDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        var consumerContext = new ConsumerContext(descriptor, message.Origin);
        var tracingTimestamp = TracingBefore(message.Origin, descriptor.MethodInfo);
        try
        {
            var ret = await Invoker.InvokeAsync(consumerContext, cancellationToken).ConfigureAwait(false);

            TracingAfter(tracingTimestamp, message.Origin, descriptor.MethodInfo);

            if (!string.IsNullOrEmpty(ret.CallbackName))
            {
                var header = new Dictionary<string, string?>
                {
                    [Headers.CorrelationId] = message.Origin.GetId(),
                    [Headers.CorrelationSequence] = (message.Origin.GetCorrelationSequence() + 1).ToString()
                };

                await _provider.GetRequiredService<ICapPublisher>()
                    .PublishAsync(ret.CallbackName, ret.Result, header, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            //ignore
        }
        catch (Exception ex)
        {
            var e = new SubscriberExecutionFailedException(ex.Message, ex);

            TracingError(tracingTimestamp, message.Origin, descriptor.MethodInfo, e);

            e.ReThrow();
        }
    }

    #region tracing

    private long? TracingBefore(Message message, MethodInfo method)
    {
        if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.BeforeSubscriberInvoke))
        {
            var eventData = new CapEventDataSubExecute
            {
                OperationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = message.GetName(),
                Message = message,
                MethodInfo = method
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.BeforeSubscriberInvoke, eventData);

            return eventData.OperationTimestamp;
        }

        return null;
    }

    private void TracingAfter(long? tracingTimestamp, Message message, MethodInfo method)
    {
        CapEventCounterSource.Log.WriteInvokeMetrics();
        if (tracingTimestamp != null &&
            s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.AfterSubscriberInvoke))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var eventData = new CapEventDataSubExecute
            {
                OperationTimestamp = now,
                Operation = message.GetName(),
                Message = message,
                MethodInfo = method,
                ElapsedTimeMs = now - tracingTimestamp.Value
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.AfterSubscriberInvoke, eventData);
        }
    }

    private void TracingError(long? tracingTimestamp, Message message, MethodInfo? method, Exception ex)
    {
        if (tracingTimestamp != null &&
            s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.ErrorSubscriberInvoke))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var eventData = new CapEventDataSubExecute
            {
                OperationTimestamp = now,
                Operation = message.GetName(),
                Message = message,
                MethodInfo = method,
                ElapsedTimeMs = now - tracingTimestamp.Value,
                Exception = ex
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.ErrorSubscriberInvoke, eventData);
        }
    }

    #endregion
}
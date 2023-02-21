// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Internal;

internal class CapPublisher : ICapPublisher
{
    // ReSharper disable once InconsistentNaming
    protected static readonly DiagnosticListener s_diagnosticListener =
        new(CapDiagnosticListenerNames.DiagnosticListenerName);

    private readonly CapOptions _capOptions;
    private readonly IDispatcher _dispatcher;
    private readonly IDataStorage _storage;
    private readonly IBootstrapper _bootstrapper;

    public CapPublisher(IServiceProvider service)
    {
        ServiceProvider = service;
        _bootstrapper = service.GetRequiredService<IBootstrapper>();
        _dispatcher = service.GetRequiredService<IDispatcher>();
        _storage = service.GetRequiredService<IDataStorage>();
        _capOptions = service.GetRequiredService<IOptions<CapOptions>>().Value;
        Transaction = new AsyncLocal<ICapTransaction>();
    }

    public IServiceProvider ServiceProvider { get; }

    public AsyncLocal<ICapTransaction> Transaction { get; }

    public async Task PublishAsync<T>(string name, T? value, IDictionary<string, string?> headers,
        CancellationToken cancellationToken = default)
    {
        await PublishInternalAsync(name, value, headers, null, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishAsync<T>(string name, T? value, string? callbackName = null,
        CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, string?>
        {
            { Headers.CallbackName, callbackName }
        };
        await PublishAsync(name, value, headers, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? value, IDictionary<string, string?> headers,
        CancellationToken cancellationToken = default)
    {
        if (delayTime <= TimeSpan.Zero)
        {
            throw new ArgumentException("Delay time span must be greater than 0", nameof(delayTime));
        }

        await PublishInternalAsync(name, value, headers, delayTime, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? value, string? callbackName = null,
        CancellationToken cancellationToken = default)
    {
        var header = new Dictionary<string, string?>
        {
            { Headers.CallbackName, callbackName }
        };

        await PublishDelayAsync(delayTime, name, value, header, cancellationToken).ConfigureAwait(false);
    }

    public void Publish<T>(string name, T? value, string? callbackName = null)
    {
        PublishAsync(name, value, callbackName).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public void Publish<T>(string name, T? value, IDictionary<string, string?> headers)
    {
        PublishAsync(name, value, headers).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public void PublishDelay<T>(TimeSpan delayTime, string name, T? value, IDictionary<string, string?> headers)
    {
        PublishDelayAsync(delayTime, name, value, headers).ConfigureAwait(false);
    }

    public void PublishDelay<T>(TimeSpan delayTime, string name, T? value, string? callbackName = null)
    {
        PublishDelayAsync(delayTime, name, value, callbackName).ConfigureAwait(false);
    }

    private async Task PublishInternalAsync<T>(string name, T? value, IDictionary<string, string?> headers, TimeSpan? delayTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!_bootstrapper.IsStarted)
        {
            throw new InvalidOperationException("CAP has not been started!");
        }

        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

        if (!string.IsNullOrEmpty(_capOptions.TopicNamePrefix)) name = $"{_capOptions.TopicNamePrefix}.{name}";

        if (!headers.ContainsKey(Headers.MessageId))
        {
            var messageId = SnowflakeId.Default().NextId().ToString();
            headers.Add(Headers.MessageId, messageId);
        }

        if (!headers.ContainsKey(Headers.CorrelationId))
        {
            headers.Add(Headers.CorrelationId, headers[Headers.MessageId]);
            headers.Add(Headers.CorrelationSequence, 0.ToString());
        }

        headers.Add(Headers.MessageName, name);
        headers.Add(Headers.Type, typeof(T).Name);

        var publishTime = DateTime.Now;
        if (delayTime != null)
        {
            publishTime += delayTime.Value;
            headers.Add(Headers.DelayTime, delayTime.Value.ToString());
            headers.Add(Headers.SentTime, publishTime.ToString());
        }
        else
        {
            headers.Add(Headers.SentTime, publishTime.ToString());
        }

        var message = new Message(headers, value);

        long? tracingTimestamp = null;
        try
        {
            tracingTimestamp = TracingBefore(message);

            if (Transaction.Value?.DbTransaction == null)
            {
                var mediumMessage = await _storage.StoreMessageAsync(name, message).ConfigureAwait(false);

                TracingAfter(tracingTimestamp, message);

                if (delayTime != null)
                {
                    await _dispatcher.EnqueueToScheduler(mediumMessage, publishTime).ConfigureAwait(false);
                }
                else
                {
                    await _dispatcher.EnqueueToPublish(mediumMessage).ConfigureAwait(false);
                }
            }
            else
            {
                var transaction = (CapTransactionBase)Transaction.Value;

                var mediumMessage = await _storage.StoreMessageAsync(name, message, transaction.DbTransaction)
                    .ConfigureAwait(false);

                TracingAfter(tracingTimestamp, message);

                transaction.AddToSent(mediumMessage);

                if (transaction.AutoCommit) await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            TracingError(tracingTimestamp, message, e);

            throw;
        }
    }

    #region tracing

    private long? TracingBefore(Message message)
    {
        if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.BeforePublishMessageStore))
        {
            var eventData = new CapEventDataPubStore
            {
                OperationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = message.GetName(),
                Message = message
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.BeforePublishMessageStore, eventData);

            return eventData.OperationTimestamp;
        }

        return null;
    }

    private void TracingAfter(long? tracingTimestamp, Message message)
    {
        if (tracingTimestamp != null &&
            s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.AfterPublishMessageStore))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var eventData = new CapEventDataPubStore
            {
                OperationTimestamp = now,
                Operation = message.GetName(),
                Message = message,
                ElapsedTimeMs = now - tracingTimestamp.Value
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.AfterPublishMessageStore, eventData);
        }
    }

    private void TracingError(long? tracingTimestamp, Message message, Exception ex)
    {
        if (tracingTimestamp != null &&
            s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.ErrorPublishMessageStore))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var eventData = new CapEventDataPubStore
            {
                OperationTimestamp = now,
                Operation = message.GetName(),
                Message = message,
                ElapsedTimeMs = now - tracingTimestamp.Value,
                Exception = ex
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.ErrorPublishMessageStore, eventData);
        }
    }

    #endregion
}
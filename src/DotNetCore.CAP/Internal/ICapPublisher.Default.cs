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

namespace DotNetCore.CAP.Internal
{
    internal class CapPublisher : ICapPublisher
    {
        private readonly IDispatcher _dispatcher;
        private readonly IDataStorage _storage;
        private readonly CapOptions _capOptions;

        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener = new(CapDiagnosticListenerNames.DiagnosticListenerName);

        public CapPublisher(IServiceProvider service)
        {
            ServiceProvider = service;
            _dispatcher = service.GetRequiredService<IDispatcher>();
            _storage = service.GetRequiredService<IDataStorage>();
            _capOptions = service.GetRequiredService<IOptions<CapOptions>>().Value;
            Transaction = new AsyncLocal<ICapTransaction>();
        }

        public IServiceProvider ServiceProvider { get; }

        public AsyncLocal<ICapTransaction> Transaction { get; }

        public Task PublishAsync<T>(string name, T? value, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Publish(name, value, headers), cancellationToken);
        }

        public Task PublishAsync<T>(string name, T? value, string? callbackName = null,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Publish(name, value, callbackName), cancellationToken);
        }

        public void Publish<T>(string name, T? value, string? callbackName = null)
        {
            var header = new Dictionary<string, string?>
            {
                {Headers.CallbackName, callbackName}
            };

            Publish(name, value, header);
        }

        public void Publish<T>(string name, T? value, IDictionary<string, string?> headers)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!string.IsNullOrEmpty(_capOptions.TopicNamePrefix))
            {
                name = $"{_capOptions.TopicNamePrefix}.{name}";
            }

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
            headers.Add(Headers.SentTime, DateTimeOffset.Now.ToString());

            var message = new Message(headers, value);

            long? tracingTimestamp = null;
            try
            {
                tracingTimestamp = TracingBefore(message);

                if (Transaction.Value?.DbTransaction == null)
                {
                    var mediumMessage = _storage.StoreMessage(name, message);

                    TracingAfter(tracingTimestamp, message);

                    _dispatcher.EnqueueToPublish(mediumMessage);
                }
                else
                {
                    var transaction = (CapTransactionBase)Transaction.Value;

                    var mediumMessage = _storage.StoreMessage(name, message, transaction.DbTransaction);

                    TracingAfter(tracingTimestamp, message);

                    transaction.AddToSent(mediumMessage);

                    if (transaction.AutoCommit)
                    {
                        transaction.Commit();
                    }
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
                var eventData = new CapEventDataPubStore()
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
            if (tracingTimestamp != null && s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.AfterPublishMessageStore))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var eventData = new CapEventDataPubStore()
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
            if (tracingTimestamp != null && s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.ErrorPublishMessageStore))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var eventData = new CapEventDataPubStore()
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
}
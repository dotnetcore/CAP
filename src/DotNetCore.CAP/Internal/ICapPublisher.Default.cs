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

namespace DotNetCore.CAP.Internal
{
    internal class CapPublisher : ICapPublisher
    {
        private readonly IDispatcher _dispatcher;
        private readonly IDataStorage _storage;

        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerNames.DiagnosticListenerName);

        public CapPublisher(IServiceProvider service)
        {
            ServiceProvider = service;
            _dispatcher = service.GetRequiredService<IDispatcher>();
            _storage = service.GetRequiredService<IDataStorage>();
            Transaction = new AsyncLocal<ICapTransaction>();
        }

        public IServiceProvider ServiceProvider { get; }

        public AsyncLocal<ICapTransaction> Transaction { get; }

        public async Task PublishAsync<T>(string name, T value,
            IDictionary<string, string> optionHeaders,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (optionHeaders == null)
            {
                optionHeaders = new Dictionary<string, string>();
            }

            var messageId = SnowflakeId.Default().NextId().ToString();
            optionHeaders.Add(Headers.MessageId, messageId);
            optionHeaders.Add(Headers.MessageName, name);
            optionHeaders.Add(Headers.Type, typeof(T).FullName);
            optionHeaders.Add(Headers.SentTime, DateTimeOffset.Now.ToString());
            if (!optionHeaders.ContainsKey(Headers.CorrelationId))
            {
                optionHeaders.Add(Headers.CorrelationId, messageId);
                optionHeaders.Add(Headers.CorrelationSequence, 0.ToString());
            }

            var message = new Message(optionHeaders, value);

            long? tracingTimestamp = null;
            try
            {
                tracingTimestamp = TracingBefore(message);

                if (Transaction.Value?.DbTransaction == null)
                {
                    var mediumMessage = await _storage.StoreMessageAsync(name, message, cancellationToken: cancellationToken);

                    TracingAfter(tracingTimestamp, message);

                    _dispatcher.EnqueueToPublish(mediumMessage);
                }
                else
                {
                    var transaction = (CapTransactionBase)Transaction.Value;

                    var mediumMessage = await _storage.StoreMessageAsync(name, message, transaction.DbTransaction, cancellationToken);

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

        public void Publish<T>(string name, T value, string callbackName = null)
        {
            PublishAsync(name, value, callbackName).GetAwaiter().GetResult();
        }

        public Task PublishAsync<T>(string name, T value, string callbackName = null,
            CancellationToken cancellationToken = default)
        {
            var header = new Dictionary<string, string>
            {
                {Headers.CallbackName, callbackName}
            };

            return PublishAsync(name, value, header, cancellationToken);
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
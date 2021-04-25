using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace DotNetCore.CAP.Internal
{
    public class Publisher<T>:  IPublisher<T>
    {
        private readonly IDispatcher _dispatcher;
        private readonly IDataStorage _storage;
        private readonly CapOptions _capOptions;
        private readonly ISerializerRegistry _serializerRegistry;


        public IServiceProvider ServiceProvider { get; }
        public AsyncLocal<ICapTransaction> Transaction { get; }

        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerNames.DiagnosticListenerName);

        public Publisher(IServiceProvider service)
        {
            ServiceProvider = service;
            _dispatcher = service.GetRequiredService<IDispatcher>();
            _storage = service.GetRequiredService<IDataStorage>();
            _capOptions = service.GetService<IOptions<CapOptions>>().Value;
            Transaction = new AsyncLocal<ICapTransaction>();
            _serializerRegistry = service.GetRequiredService<ISerializerRegistry>();
        }

        public Task PublishAsync(string name, T value, IDictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Publish(name, value, headers), cancellationToken);
        }

        public Task PublishAsync(string name, T value, string callbackName = null,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Publish(name, value, callbackName), cancellationToken);
        }

        public void Publish(string name, T value, string callbackName = null)
        {
            var header = new Dictionary<string, string>
            {
                {Headers.CallbackName, callbackName}
            };

            Publish(name, value, header);
        }

        public void Publish(string name, T value, IDictionary<string, string> headers)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!string.IsNullOrEmpty(_capOptions.TopicNamePrefix))
            {
                name = $"{_capOptions.TopicNamePrefix}.{name}";
            }

            headers ??= new Dictionary<string, string>();

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
            headers.Add(Headers.Type, typeof(T).AssemblyQualifiedName);
            headers.Add(Headers.SentTime, DateTimeOffset.Now.ToString());

            //  them message with T generic
            var message = new Message<T>(headers, value);

            long? tracingTimestamp = null;
            try
            {
                tracingTimestamp = TracingBefore(message);

                if (Transaction.Value?.DbTransaction == null)
                {
                    var mediumMessage = _storage.StoreMessage<T>(name, message);

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


        private long? TracingBefore(ICapMessage message)
        {
            if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.BeforePublishMessageStore))
            {
                var eventData = new CapEventDataPubStore<T>()
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


        private void TracingAfter(long? tracingTimestamp, ICapMessage message)
        {
            if (tracingTimestamp != null && s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.AfterPublishMessageStore))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var eventData = new CapEventDataPubStore<T>()
                {
                    OperationTimestamp = now,
                    Operation = message.GetName(),
                    Message = message,
                    ElapsedTimeMs = now - tracingTimestamp.Value
                };

                s_diagnosticListener.Write(CapDiagnosticListenerNames.AfterPublishMessageStore, eventData);
            }
        }

        private void TracingError(long? tracingTimestamp, ICapMessage message, Exception ex)
        {
            if (tracingTimestamp != null && s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.ErrorPublishMessageStore))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var eventData = new CapEventDataPubStore<T>()
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

    }
}

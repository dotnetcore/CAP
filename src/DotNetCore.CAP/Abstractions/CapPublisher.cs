// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Abstractions
{
    public class CapPublisher : ICapPublisher
    {
        private readonly IDispatcher _dispatcher;
        private readonly IDataStorage _storage;

        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

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
            if (!optionHeaders.ContainsKey(Headers.CorrelationId))
            {
                optionHeaders.Add(Headers.CorrelationId, messageId);
                optionHeaders.Add(Headers.CorrelationSequence, 0.ToString());
            }
            optionHeaders.Add(Headers.MessageName, name);
            optionHeaders.Add(Headers.Type, typeof(T).ToString());
            optionHeaders.Add(Headers.SentTime, DateTimeOffset.Now.ToString());

            var message = new Message(optionHeaders, value);

            var operationId = default(Guid);
            try
            {
                operationId = s_diagnosticListener.WritePublishMessageStoreBefore(message);

                if (Transaction.Value?.DbTransaction == null)
                {
                    var mediumMessage = await _storage.StoreMessageAsync(name, message, cancellationToken: cancellationToken);

                    s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);

                    _dispatcher.EnqueueToPublish(mediumMessage);
                }
                else
                {
                    var transaction = (CapTransactionBase)Transaction.Value;

                    var mediumMessage = await _storage.StoreMessageAsync(name, message, transaction, cancellationToken);

                    s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);

                    transaction.AddToSent(mediumMessage);

                    if (transaction.AutoCommit)
                    {
                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                s_diagnosticListener.WritePublishMessageStoreError(operationId, message, e);
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
    }
}
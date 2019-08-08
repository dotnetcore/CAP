// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Abstractions
{
    public abstract class CapPublisherBase : ICapPublisher
    {
        private readonly IMessagePacker _msgPacker;
        private readonly IContentSerializer _serializer;
        private readonly IDispatcher _dispatcher;

        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        protected CapPublisherBase(IServiceProvider service)
        {
            ServiceProvider = service;
            _dispatcher = service.GetRequiredService<IDispatcher>();
            _msgPacker = service.GetRequiredService<IMessagePacker>();
            _serializer = service.GetRequiredService<IContentSerializer>();
            Transaction = new AsyncLocal<ICapTransaction>();
        }

        public IServiceProvider ServiceProvider { get; }

        public AsyncLocal<ICapTransaction> Transaction { get; }

        public void Publish<T>(string name, T contentObj, string callbackName = null)
        {
            var message = new CapPublishedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                Name = name,
                Content = Serialize(contentObj, callbackName),
                StatusName = StatusName.Scheduled
            };

            PublishAsyncInternal(message).GetAwaiter().GetResult();
        }

        public async Task PublishAsync<T>(string name, T contentObj, string callbackName = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = new CapPublishedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                Name = name,
                Content = Serialize(contentObj, callbackName),
                StatusName = StatusName.Scheduled
            };

            await PublishAsyncInternal(message);
        }

        protected async Task PublishAsyncInternal(CapPublishedMessage message)
        {
            var operationId = default(Guid);

            try
            {
                var tracingResult = TracingBefore(message.Name, message.Content);
                operationId = tracingResult.Item1;
                
                message.Content = tracingResult.Item2 != null
                    ? Helper.AddTracingHeaderProperty(message.Content, tracingResult.Item2)
                    : message.Content;

                if (Transaction.Value?.DbTransaction == null)
                {
                    await ExecuteAsync(message);

                    s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);

                    _dispatcher.EnqueueToPublish(message);
                }
                else
                {
                    var transaction = (CapTransactionBase)Transaction.Value;

                    await ExecuteAsync(message, transaction);

                    s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);
                   
                    transaction.AddToSent(message);
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
        
        private (Guid, TracingHeaders) TracingBefore(string topic, string values)
        {
            Guid operationId = Guid.NewGuid();
            
            var eventData = new BrokerStoreEventData(
                operationId, 
                "",
                topic, 
                values);

            s_diagnosticListener.WritePublishMessageStoreBefore(eventData);

            return (operationId, eventData.Headers);
        }

        protected abstract Task ExecuteAsync(CapPublishedMessage message,
            ICapTransaction transaction = null,
            CancellationToken cancel = default(CancellationToken));

        protected virtual string Serialize<T>(T obj, string callbackName = null)
        {
            string content;
            if (obj != null)
            {
                content = Helper.IsComplexType(obj.GetType())
                    ? _serializer.Serialize(obj)
                    : obj.ToString();
            }
            else
            {
                content = string.Empty;
            }
            var message = new CapMessageDto(content)
            {
                CallbackName = callbackName
            };
            return _msgPacker.Pack(message);
        }
    }
}
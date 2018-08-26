// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Abstractions
{
    public abstract class CapPublisherBase : ICapPublisher
    {
        private readonly CapTransactionBase _transaction;
        private readonly IMessagePacker _msgPacker;
        private readonly IContentSerializer _serializer;

        protected bool NotUseTransaction;

        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        protected CapPublisherBase(IServiceProvider service)
        {
            ServiceProvider = service;
            _transaction = service.GetRequiredService<CapTransactionBase>();
            _msgPacker = service.GetRequiredService<IMessagePacker>();
            _serializer = service.GetRequiredService<IContentSerializer>();
        }

        protected IServiceProvider ServiceProvider { get; }

        public ICapTransaction Transaction => _transaction;

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
            if (Transaction.DbTransaction == null)
            {
                NotUseTransaction = true;
                Transaction.DbTransaction = new NoopTransaction();
            }

            Guid operationId = default(Guid);

            try
            {
                operationId = s_diagnosticListener.WritePublishMessageStoreBefore(message);

                await ExecuteAsync(message, Transaction);

                _transaction.AddToSent(message);

                s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);

                if (NotUseTransaction || Transaction.AutoCommit)
                {
                    _transaction.Commit();
                }
            }
            catch (Exception e)
            {
                s_diagnosticListener.WritePublishMessageStoreError(operationId, message, e);

                throw;
            }
            finally
            {
                if (NotUseTransaction || Transaction.AutoCommit)
                {
                    _transaction.Dispose();
                }
            }
        }

        protected abstract Task ExecuteAsync(CapPublishedMessage message,
            ICapTransaction transaction,
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
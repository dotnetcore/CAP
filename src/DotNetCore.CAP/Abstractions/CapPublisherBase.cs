// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Abstractions
{
    public abstract class CapPublisherBase : ICapPublisher, IDisposable
    {
        private readonly IDispatcher _dispatcher;
        private readonly ILogger _logger;

        // diagnostics listener
        // ReSharper disable once InconsistentNaming
        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        protected CapPublisherBase(ILogger<CapPublisherBase> logger, IDispatcher dispatcher)
        {
            _logger = logger;
            _dispatcher = dispatcher;
        }

        protected IDbConnection DbConnection { get; set; }
        protected IDbTransaction DbTransaction { get; set; }
        protected bool IsCapOpenedTrans { get; set; }
        protected bool IsCapOpenedConn { get; set; }
        protected bool IsUsingEF { get; set; }
        protected IServiceProvider ServiceProvider { get; set; }

        public void Publish<T>(string name, T contentObj, string callbackName = null)
        {
            CheckIsUsingEF(name);
            PrepareConnectionForEF();

            PublishWithTrans(name, contentObj, callbackName);
        }

        public Task PublishAsync<T>(string name, T contentObj, string callbackName = null)
        {
            CheckIsUsingEF(name);
            PrepareConnectionForEF();

            return PublishWithTransAsync(name, contentObj, callbackName);
        }

        public void Publish<T>(string name, T contentObj, IDbTransaction dbTransaction, string callbackName = null)
        {
            CheckIsAdoNet(name);
            PrepareConnectionForAdo(dbTransaction);

            PublishWithTrans(name, contentObj, callbackName);
        }

        public Task PublishAsync<T>(string name, T contentObj, IDbTransaction dbTransaction, string callbackName = null)
        {
            CheckIsAdoNet(name);
            PrepareConnectionForAdo(dbTransaction);

            return PublishWithTransAsync(name, contentObj, callbackName);
        }

        protected void Enqueue(CapPublishedMessage message)
        {
            _dispatcher.EnqueueToPublish(message);
        }

        protected abstract void PrepareConnectionForEF();

        protected abstract int Execute(IDbConnection dbConnection, IDbTransaction dbTransaction,
            CapPublishedMessage message);

        protected abstract Task<int> ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction,
            CapPublishedMessage message);

        protected virtual string Serialize<T>(T obj, string callbackName = null)
        {
            var packer = (IMessagePacker)ServiceProvider.GetService(typeof(IMessagePacker));
            string content;
            if (obj != null)
            {
                if (Helper.IsComplexType(obj.GetType()))
                {
                    var serializer = (IContentSerializer)ServiceProvider.GetService(typeof(IContentSerializer));
                    content = serializer.Serialize(obj);
                }
                else
                {
                    content = obj.ToString();
                }
            }
            else
            {
                content = string.Empty;
            }

            var message = new CapMessageDto(content)
            {
                CallbackName = callbackName
            };
            return packer.Pack(message);
        }

        #region private methods

        private void PrepareConnectionForAdo(IDbTransaction dbTransaction)
        {
            DbTransaction = dbTransaction ?? throw new ArgumentNullException(nameof(dbTransaction));
            DbConnection = DbTransaction.Connection;
            if (DbConnection.State != ConnectionState.Open)
            {
                IsCapOpenedConn = true;
                DbConnection.Open();
            }
        }

        private void CheckIsUsingEF(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!IsUsingEF)
            {
                throw new InvalidOperationException(
                    "If you are using the EntityFramework, you need to configure the DbContextType first." +
                    " otherwise you need to use overloaded method with IDbTransaction.");
            }
        }

        private void CheckIsAdoNet(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (IsUsingEF)
            {
                throw new InvalidOperationException(
                    "If you are using the EntityFramework, you do not need to use this overloaded.");
            }
        }

        private async Task PublishWithTransAsync<T>(string name, T contentObj, string callbackName = null)
        {
            Guid operationId = default(Guid);
            var content = Serialize(contentObj, callbackName);

            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            try
            {
                operationId = s_diagnosticListener.WritePublishMessageStoreBefore(message);

                var id = await ExecuteAsync(DbConnection, DbTransaction, message);

                ClosedCap();

                if (id > 0)
                {
                    _logger.LogInformation($"message [{message}] has been persisted in the database.");
                    s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);

                    message.Id = id;

                    Enqueue(message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception was occurred when publish message async. exception message:" + name);
                s_diagnosticListener.WritePublishMessageStoreError(operationId, message, e);
                Console.WriteLine(e);
                throw;
            }
        }

        private void PublishWithTrans<T>(string name, T contentObj, string callbackName = null)
        {
            Guid operationId = default(Guid);

            var content = Serialize(contentObj, callbackName);

            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            try
            {
                Console.WriteLine("================22222222222222=====================");
                operationId = s_diagnosticListener.WritePublishMessageStoreBefore(message);               

                var id = Execute(DbConnection, DbTransaction, message);
                Console.WriteLine("================777777777777777777777=====================");
                ClosedCap();

                if (id > 0)
                {
                    _logger.LogInformation($"message [{message}] has been persisted in the database.");
                    s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message);
                    message.Id = id;
                    Enqueue(message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception was occurred when publish message. message:" + name);
                s_diagnosticListener.WritePublishMessageStoreError(operationId, message, e);
                Console.WriteLine(e);
                throw;
            }
        }

        private void ClosedCap()
        {
            if (IsCapOpenedTrans)
            {
                DbTransaction.Commit();
                DbTransaction.Dispose();
            }

            if (IsCapOpenedConn)
            {
                DbConnection.Dispose();
            }
        }

        public void Dispose()
        {
            DbTransaction?.Dispose();
            DbConnection?.Dispose();
        }

        #endregion private methods
    }
}
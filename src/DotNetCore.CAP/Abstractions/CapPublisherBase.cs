using System;
using System.Data;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Abstractions
{
    public abstract class CapPublisherBase : ICapPublisher, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IDispatcher _dispatcher;

        protected CapPublisherBase(ILogger logger, IDispatcher dispatcher)
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

        protected void Enqueu(CapPublishedMessage message)
        {
            _dispatcher.EnqueuToPublish(message);
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
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (!IsUsingEF)
                throw new InvalidOperationException(
                    "If you are using the EntityFramework, you need to configure the DbContextType first." +
                    " otherwise you need to use overloaded method with IDbConnection and IDbTransaction.");
        }

        private void CheckIsAdoNet(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (IsUsingEF)
                throw new InvalidOperationException(
                    "If you are using the EntityFramework, you do not need to use this overloaded.");
        }

        private async Task PublishWithTransAsync<T>(string name, T contentObj, string callbackName = null)
        {
            try
            {
                var content = Serialize(contentObj, callbackName);

                var message = new CapPublishedMessage
                {
                    Name = name,
                    Content = content,
                    StatusName = StatusName.Scheduled
                };

                var id = await ExecuteAsync(DbConnection, DbTransaction, message);

                ClosedCap();

                if (id > 0)
                {
                    _logger.LogInformation($"message [{message}] has been persisted in the database.");

                    message.Id = id;

                    Enqueu(message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("An exception was occurred when publish message. exception message:" + e.Message, e);
                Console.WriteLine(e);
                throw;
            }
        }

        private void PublishWithTrans<T>(string name, T contentObj, string callbackName = null)
        {
            try
            {
                var content = Serialize(contentObj, callbackName);

                var message = new CapPublishedMessage
                {
                    Name = name,
                    Content = content,
                    StatusName = StatusName.Scheduled
                };

                var id = Execute(DbConnection, DbTransaction, message);

                ClosedCap();

                if (id > 0)
                {
                    _logger.LogInformation($"message [{message}] has been persisted in the database.");

                    message.Id = id;

                    Enqueu(message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("An exception was occurred when publish message. exception message:" + e.Message, e);
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
                DbConnection.Dispose();
        }

        public void Dispose()
        {
            DbTransaction?.Dispose();
            DbConnection?.Dispose();
        }

        #endregion private methods
    }
}
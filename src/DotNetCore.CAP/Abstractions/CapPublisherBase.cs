using System;
using System.Data;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;

namespace DotNetCore.CAP.Abstractions
{
    public abstract class CapPublisherBase : ICapPublisher, IDisposable
    {
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

            var content = Serialize(contentObj, callbackName);

            PublishWithTrans(name, content);
        }

        public Task PublishAsync<T>(string name, T contentObj, string callbackName = null)
        {
            CheckIsUsingEF(name);
            PrepareConnectionForEF();

            var content = Serialize(contentObj, callbackName);

            return PublishWithTransAsync(name, content);
        }

        public void Publish<T>(string name, T contentObj, IDbTransaction dbTransaction, string callbackName = null)
        {
            CheckIsAdoNet(name);
            PrepareConnectionForAdo(dbTransaction);

            var content = Serialize(contentObj, callbackName);

            PublishWithTrans(name, content);
        }

        public Task PublishAsync<T>(string name, T contentObj, IDbTransaction dbTransaction, string callbackName = null)
        {
            CheckIsAdoNet(name);
            PrepareConnectionForAdo(dbTransaction);

            var content = Serialize(contentObj, callbackName);

            return PublishWithTransAsync(name, content);
        }

        protected abstract void PrepareConnectionForEF();

        protected abstract void Execute(IDbConnection dbConnection, IDbTransaction dbTransaction,
            CapPublishedMessage message);

        protected abstract Task ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction,
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

        private async Task PublishWithTransAsync(string name, string content)
        {
            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            await ExecuteAsync(DbConnection, DbTransaction, message);

            ClosedCap();

            PublishQueuer.PulseEvent.Set();
        }

        private void PublishWithTrans(string name, string content)
        {
            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            Execute(DbConnection, DbTransaction, message);

            ClosedCap();

            PublishQueuer.PulseEvent.Set();
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
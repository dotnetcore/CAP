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
        protected IDbTransaction DbTranasaction { get; set; }
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

        public void Publish<T>(string name, T contentObj, IDbConnection dbConnection,
            string callbackName = null, IDbTransaction dbTransaction = null)
        {
            CheckIsAdoNet(name);
            PrepareConnectionForAdo(dbConnection, dbTransaction);

            var content = Serialize(contentObj, callbackName);

            PublishWithTrans(name, content);
        }

        public Task PublishAsync<T>(string name, T contentObj, IDbConnection dbConnection,
            string callbackName = null, IDbTransaction dbTransaction = null)
        {
            CheckIsAdoNet(name);
            PrepareConnectionForAdo(dbConnection, dbTransaction);

            var content = Serialize(contentObj, callbackName);

            return PublishWithTransAsync(name, content);
        }

        protected abstract void PrepareConnectionForEF();

        protected abstract void Execute(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message);

        protected abstract Task ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message);

        #region private methods

        private string Serialize<T>(T obj, string callbackName = null)
        {
            var message = new Message(obj)
            {
                CallbackName = callbackName
            };

            return Helper.ToJson(message);
        }

        private void PrepareConnectionForAdo(IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            DbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
            if (DbConnection.State != ConnectionState.Open)
            {
                IsCapOpenedConn = true;
                DbConnection.Open();
            }
            DbTranasaction = dbTransaction;
            if (DbTranasaction == null)
            {
                IsCapOpenedTrans = true;
                DbTranasaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
            }
        }

        private void CheckIsUsingEF(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (!IsUsingEF)
                throw new InvalidOperationException("If you are using the EntityFramework, you need to configure the DbContextType first." +
                  " otherwise you need to use overloaded method with IDbConnection and IDbTransaction.");
        }

        private void CheckIsAdoNet(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (IsUsingEF)
                throw new InvalidOperationException("If you are using the EntityFramework, you do not need to use this overloaded.");
        }

        private async Task PublishWithTransAsync(string name, string content)
        {
            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            await ExecuteAsync(DbConnection, DbTranasaction, message);

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

            Execute(DbConnection, DbTranasaction, message);

            ClosedCap();

            PublishQueuer.PulseEvent.Set();
        }

        private void ClosedCap()
        {
            if (IsCapOpenedTrans)
            {
                DbTranasaction.Commit();
                DbTranasaction.Dispose();
            }
            if (IsCapOpenedConn)
            {
                DbConnection.Dispose();
            }
        }

        public void Dispose()
        {
            DbTranasaction?.Dispose();
            DbConnection?.Dispose();
        }

        #endregion private methods
    }
}

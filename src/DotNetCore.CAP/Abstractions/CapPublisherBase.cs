using System;
using System.Data;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;

namespace DotNetCore.CAP.Abstractions
{
    public abstract class CapPublisherBase : ICapPublisher
    {
        protected IDbConnection DbConnection { get; set; }
        protected IDbTransaction DbTranasaction { get; set; }
        protected bool IsCapOpenedTrans { get; set; }
        protected bool IsUsingEF { get; set; }
        protected IServiceProvider ServiceProvider { get; set; }
         
        public void Publish<T>(string name, T contentObj)
        {
            CheckIsUsingEF(name);
            PrepareConnectionForEF();

            var content = Serialize(contentObj);

            PublishWithTrans(name, content, DbConnection, DbTranasaction);
        }

        public Task PublishAsync<T>(string name, T contentObj)
        {
            CheckIsUsingEF(name);
            PrepareConnectionForEF();

            var content = Serialize(contentObj);

            return PublishWithTransAsync(name, content, DbConnection, DbTranasaction);
        }

        public void Publish<T>(string name, T contentObj, IDbConnection dbConnection, IDbTransaction dbTransaction = null)
        {
            CheckIsAdoNet(name);
            PrepareConnectionForAdo(dbConnection, ref dbTransaction);

            var content = Serialize(contentObj);

            PublishWithTrans(name, content, dbConnection, dbTransaction);
        }

        public Task PublishAsync<T>(string name, T contentObj, IDbConnection dbConnection, IDbTransaction dbTransaction = null)
        {
            CheckIsAdoNet(name);
            PrepareConnectionForAdo(dbConnection, ref dbTransaction);

            var content = Serialize(contentObj);

            return PublishWithTransAsync(name, content, dbConnection, dbTransaction);
        }

        protected abstract void PrepareConnectionForEF();

        protected abstract void Execute(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message);

        protected abstract Task ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message);

        #region private methods

        private string Serialize<T>(T obj)
        {
            string content = string.Empty;
            if (Helper.IsComplexType(typeof(T)))
            {
                content = Helper.ToJson(obj);
            }
            else
            {
                content = obj.ToString();
            }
            return content;
        }

        private void PrepareConnectionForAdo(IDbConnection dbConnection, ref IDbTransaction dbTransaction)
        {
            if (dbConnection == null)
                throw new ArgumentNullException(nameof(dbConnection));

            if (dbConnection.State != ConnectionState.Open)
                dbConnection.Open();

            if (dbTransaction == null)
            {
                IsCapOpenedTrans = true;
                dbTransaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
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

        private async Task PublishWithTransAsync(string name, string content, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            await ExecuteAsync(dbConnection, dbTransaction, message);

            if (IsCapOpenedTrans)
            {
                dbTransaction.Commit();
                dbTransaction.Dispose();
                dbConnection.Dispose();
            }
            PublishQueuer.PulseEvent.Set();
        }

        private void PublishWithTrans(string name, string content, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            Execute(dbConnection, dbTransaction, message);

            if (IsCapOpenedTrans)
            {
                dbTransaction.Commit();
                dbTransaction.Dispose();
                dbConnection.Dispose();
            }
            PublishQueuer.PulseEvent.Set();
        }

        #endregion private methods
    }
}

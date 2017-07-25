using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.MySql
{
    public class CapPublisher : ICapPublisher
    {
        private readonly ILogger _logger;
        private readonly MySqlOptions _options;
        private readonly DbContext _dbContext;

        protected bool IsCapOpenedTrans { get; set; }

        protected bool IsUsingEF { get; }

        protected IServiceProvider ServiceProvider { get; }

        public CapPublisher(IServiceProvider provider,
            ILogger<CapPublisher> logger,
            MySqlOptions options)
        {
            ServiceProvider = provider;
            _logger = logger;
            _options = options;

            if (_options.DbContextType != null)
            {
                IsUsingEF = true;
                _dbContext = (DbContext)ServiceProvider.GetService(_options.DbContextType);
            }
        }

        public void Publish(string name, string content)
        {
            CheckIsUsingEF(name);

            PublishCore(name, content);
        }

        public Task PublishAsync(string name, string content)
        {
            CheckIsUsingEF(name);

            return PublishCoreAsync(name, content);
        }

        public void Publish<T>(string name, T contentObj)
        {
            CheckIsUsingEF(name);

            var content = Helper.ToJson(contentObj);

            PublishCore(name, content);
        }

        public Task PublishAsync<T>(string name, T contentObj)
        {
            CheckIsUsingEF(name);

            var content = Helper.ToJson(contentObj);

            return PublishCoreAsync(name, content);
        }

        public void Publish(string name, string content, IDbConnection dbConnection, IDbTransaction dbTransaction = null)
        {
            CheckIsAdoNet(name);

            PrepareConnection(dbConnection, ref dbTransaction);

            PublishWithTrans(name, content, dbConnection, dbTransaction);
        }

        public Task PublishAsync(string name, string content, IDbConnection dbConnection, IDbTransaction dbTransaction = null)
        {
            CheckIsAdoNet(name);

            PrepareConnection(dbConnection, ref dbTransaction);

            return PublishWithTransAsync(name, content, dbConnection, dbTransaction);
        }

        public void Publish<T>(string name, T contentObj, IDbConnection dbConnection, IDbTransaction dbTransaction = null)
        {
            CheckIsAdoNet(name);

            PrepareConnection(dbConnection, ref dbTransaction);

            var content = Helper.ToJson(contentObj);

            PublishWithTrans(name, content, dbConnection, dbTransaction);
        }

        public Task PublishAsync<T>(string name, T contentObj, IDbConnection dbConnection, IDbTransaction dbTransaction = null)
        {
            CheckIsAdoNet(name);

            PrepareConnection(dbConnection, ref dbTransaction);

            var content = Helper.ToJson(contentObj);

            return PublishWithTransAsync(name, content, dbConnection, dbTransaction);
        }

        #region private methods

        private void PrepareConnection(IDbConnection dbConnection, ref IDbTransaction dbTransaction)
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

        private async Task PublishCoreAsync(string name, string content)
        {
            var connection = _dbContext.Database.GetDbConnection();
            var transaction = _dbContext.Database.CurrentTransaction;
            if (transaction == null)
            {
                IsCapOpenedTrans = true;
                transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            }
            var dbTransaction = transaction.GetDbTransaction();
            await PublishWithTransAsync(name, content, connection, dbTransaction);
        }

        private void PublishCore(string name, string content)
        {
            var connection = _dbContext.Database.GetDbConnection();
            var transaction = _dbContext.Database.CurrentTransaction;
            if (transaction == null)
            {
                IsCapOpenedTrans = true;
                transaction = _dbContext.Database.BeginTransaction(IsolationLevel.ReadCommitted);
            }
            var dbTransaction = transaction.GetDbTransaction();
            PublishWithTrans(name, content, connection, dbTransaction);
        }

        private async Task PublishWithTransAsync(string name, string content, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };
            await dbConnection.ExecuteAsync(PrepareSql(), message, transaction: dbTransaction);

            _logger.LogInformation("Message has been persisted in the database. name:" + name);

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
            var count = dbConnection.Execute(PrepareSql(), message, transaction: dbTransaction);

            _logger.LogInformation("Message has been persisted in the database. name:" + name);

            if (IsCapOpenedTrans)
            {
                dbTransaction.Commit();
                dbTransaction.Dispose();
                dbConnection.Dispose();
            }
            PublishQueuer.PulseEvent.Set();
        }

        private string PrepareSql()
        {
            return $"INSERT INTO `{_options.TableNamePrefix}.published` (`Name`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName)";
        }

        #endregion private methods
    }
}
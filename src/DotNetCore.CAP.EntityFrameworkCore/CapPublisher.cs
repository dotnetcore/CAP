using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class CapPublisher : ICapPublisher
    {
        private readonly SqlServerOptions _options;
        private readonly DbContext _dbContext;

        protected bool IsUsingEF { get; }
        protected IServiceProvider ServiceProvider { get; }

        public CapPublisher(IServiceProvider provider, SqlServerOptions options)
        {
            ServiceProvider = provider;
            _options = options;

            if (_options.DbContextType != null)
            {
                IsUsingEF = true;
                _dbContext = (DbContext)ServiceProvider.GetService(_options.DbContextType);
            }
        }

        public Task PublishAsync(string name, string content)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (!IsUsingEF) throw new InvalidOperationException("If you are using the EntityFramework, you need to configure the DbContextType first." +
                " otherwise you need to use overloaded method with IDbConnection and IDbTransaction.");

            return Publish(name, content);
        }

        public Task PublishAsync<T>(string name, T contentObj)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (!IsUsingEF) throw new InvalidOperationException("If you are using the EntityFramework, you need to configure the DbContextType first." +
                " otherwise you need to use overloaded method with IDbConnection and IDbTransaction.");

            var content = Helper.ToJson(contentObj);
            return Publish(name, content);
        }

        public Task PublishAsync(string name, string content, IDbConnection dbConnection)
        {
            if (IsUsingEF) throw new InvalidOperationException("If you are using the EntityFramework, you do not need to use this overloaded.");
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (dbConnection == null) throw new ArgumentNullException(nameof(dbConnection));

            var dbTransaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
            return PublishWithTrans(name, content, dbConnection, dbTransaction);
        }

        public Task PublishAsync(string name, string content, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            if (IsUsingEF) throw new InvalidOperationException("If you are using the EntityFramework, you do not need to use this overloaded.");
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (dbConnection == null) throw new ArgumentNullException(nameof(dbConnection));
            if (dbTransaction == null) throw new ArgumentNullException(nameof(dbTransaction));

            return PublishWithTrans(name, content, dbConnection, dbTransaction);
        }

        private async Task Publish(string name, string content)
        {
            var connection = _dbContext.Database.GetDbConnection();
            var transaction = _dbContext.Database.CurrentTransaction;
            transaction = transaction ?? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            var dbTransaction = transaction.GetDbTransaction();
            await PublishWithTrans(name, content, connection, dbTransaction);
        }

        private async Task PublishWithTrans(string name, string content, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var message = new CapPublishedMessage
            {
                Name = name,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            var sql = $"INSERT INTO {_options.Schema}.[Published] ([Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName])VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName)";
            await dbConnection.ExecuteAsync(sql, message, transaction: dbTransaction);

            PublishQueuer.PulseEvent.Set();
        }
    }
}
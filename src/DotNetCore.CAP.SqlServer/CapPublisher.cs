using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.SqlServer
{
    public class CapPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly ILogger _logger;
        private readonly SqlServerOptions _options;
        private readonly DbContext _dbContext;

        public CapPublisher(IServiceProvider provider,
            ILogger<CapPublisher> logger,
            SqlServerOptions options)
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

        protected override void PrepareConnectionForEF()
        {
            DbConnection = _dbContext.Database.GetDbConnection();
            var dbContextTransaction = _dbContext.Database.CurrentTransaction;
            var dbTrans = dbContextTransaction?.GetDbTransaction();
            //DbTransaction is dispose in original  
            if (dbTrans?.Connection == null)
            {
                IsCapOpenedTrans = true;
                dbContextTransaction?.Dispose();
                dbContextTransaction = _dbContext.Database.BeginTransaction(IsolationLevel.ReadCommitted);
                dbTrans = dbContextTransaction.GetDbTransaction();
            }
            DbTranasaction = dbTrans;
        }

        protected override void Execute(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message)
        {
            dbConnection.Execute(PrepareSql(), message, dbTransaction);

            _logger.LogInformation("Published Message has been persisted in the database. name:" + message.ToString());
        }

        protected override async Task ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message)
        {
            await dbConnection.ExecuteAsync(PrepareSql(), message, dbTransaction);

            _logger.LogInformation("Published Message has been persisted in the database. name:" + message.ToString());
        }

        public async Task PublishAsync(CapPublishedMessage message)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
               await conn.ExecuteAsync(PrepareSql(), message);
            } 
        }

        #region private methods

        private string PrepareSql()
        {
            return $"INSERT INTO {_options.Schema}.[Published] ([Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName])VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName)";
        }

        #endregion private methods
    }
}
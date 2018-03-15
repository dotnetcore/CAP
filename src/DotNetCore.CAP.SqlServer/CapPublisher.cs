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
        private readonly DbContext _dbContext;
        private readonly SqlServerOptions _options;

        public CapPublisher(ILogger<CapPublisher> logger, IDispatcher dispatcher,
            IServiceProvider provider, SqlServerOptions options)
            : base(logger, dispatcher)
        {
            ServiceProvider = provider;
            _options = options;

            if (_options.DbContextType == null) return;

            IsUsingEF = true;
            _dbContext = (DbContext)ServiceProvider.GetService(_options.DbContextType);
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                var id = await conn.ExecuteScalarAsync<int>(PrepareSql(), message);
                message.Id = id;
                Enqueu(message); 
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
            DbTransaction = dbTrans;
        }

        protected override int Execute(IDbConnection dbConnection, IDbTransaction dbTransaction,
            CapPublishedMessage message)
        {
            return dbConnection.ExecuteScalar<int>(PrepareSql(), message, dbTransaction);
        }

        protected override Task<int> ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction,
            CapPublishedMessage message)
        {
            return dbConnection.ExecuteScalarAsync<int>(PrepareSql(), message, dbTransaction);
        }

        #region private methods

        private string PrepareSql()
        {
            return
                $"INSERT INTO {_options.Schema}.[Published] ([Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName])VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);SELECT SCOPE_IDENTITY();";
        }

        #endregion private methods
    }
}
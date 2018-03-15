using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class CapPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly DbContext _dbContext;
        private readonly PostgreSqlOptions _options;

        public CapPublisher(ILogger<CapPublisher> logger, IDispatcher dispatcher,
            IServiceProvider provider, PostgreSqlOptions options)
            : base(logger, dispatcher)
        {
            ServiceProvider = provider;
            _options = options;

            if (_options.DbContextType != null)
            {
                IsUsingEF = true;
                _dbContext = (DbContext)ServiceProvider.GetService(_options.DbContextType);
            }
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            using (var conn = new NpgsqlConnection(_options.ConnectionString))
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
                $"INSERT INTO \"{_options.Schema}\".\"published\" (\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING \"Id\";";
        }

        #endregion private methods
    }
}
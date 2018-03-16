// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class CapPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly DbContext _dbContext;
        private readonly MySqlOptions _options;

        public CapPublisher(ILogger<CapPublisher> logger, IDispatcher dispatcher, IServiceProvider provider,
            MySqlOptions options)
            : base(logger, dispatcher)
        {
            ServiceProvider = provider;
            _options = options;

            if (_options.DbContextType == null)
            {
                return;
            }

            IsUsingEF = true;
            _dbContext = (DbContext) ServiceProvider.GetService(_options.DbContextType);
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            using (var conn = new MySqlConnection(_options.ConnectionString))
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

        protected override async Task<int> ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction,
            CapPublishedMessage message)
        {
            return await dbConnection.ExecuteScalarAsync<int>(PrepareSql(), message, dbTransaction);
        }

        #region private methods

        private string PrepareSql()
        {
            return
                $"INSERT INTO `{_options.TableNamePrefix}.published` (`Name`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);SELECT LAST_INSERT_ID()";
        }

        #endregion private methods
    }
}
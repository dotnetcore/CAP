// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly DbContext _dbContext;
        private readonly PostgreSqlOptions _options;
        private readonly bool _isUsingEF;

        private NpgsqlConnection _connection;

        public PostgreSqlPublisher(IServiceProvider provider, PostgreSqlOptions options): base(provider)
        {
            _options = options;

            if (_options.DbContextType == null)
            {
                return;
            }

            _isUsingEF = true;
            _dbContext = (DbContext)ServiceProvider.GetService(_options.DbContextType);
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            await PublishAsyncInternal(message);
        }

        protected override Task ExecuteAsync(CapPublishedMessage message, ICapTransaction transaction,
            CancellationToken cancel = default(CancellationToken))
        {
            var dbTrans = transaction.DbTransaction as IDbTransaction;
            if (dbTrans == null && transaction.DbTransaction is IDbContextTransaction dbContextTrans)
            {
                dbTrans = dbContextTrans.GetDbTransaction();
            }
            var conn = dbTrans?.Connection;
            return conn.ExecuteAsync(PrepareSql(), message, dbTrans);
        }

        protected override object GetDbTransaction()
        {
            if (_isUsingEF)
            {
                var dbContextTransaction = _dbContext.Database.CurrentTransaction;
                if (dbContextTransaction == null)
                {
                    return InitDbConnection();
                }

                return dbContextTransaction;
            }

            return InitDbConnection();
        }

        #region private methods

        private string PrepareSql()
        {
            return
                $"INSERT INTO \"{_options.Schema}\".\"published\" (\"Id\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")VALUES(@Id,@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
        }

        private IDbTransaction InitDbConnection()
        {
            _connection = new NpgsqlConnection(_options.ConnectionString);
            _connection.Open();
            return _connection.BeginTransaction(IsolationLevel.ReadCommitted);
        }
        #endregion private methods

        public void Dispose()
        {
            _dbContext?.Dispose();
            _connection?.Dispose();
        }
    }
}
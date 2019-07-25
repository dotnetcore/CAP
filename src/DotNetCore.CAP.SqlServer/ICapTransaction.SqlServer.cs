// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.SqlServer.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class SqlServerCapTransaction : CapTransactionBase
    {
        private readonly DbContext _dbContext;
        private readonly DiagnosticProcessorObserver _diagnosticProcessor;

        public SqlServerCapTransaction(
            IDispatcher dispatcher,
            IServiceProvider serviceProvider) : base(dispatcher)
        {
            var sqlServerOptions = serviceProvider.GetService<IOptions<SqlServerOptions>>().Value;
            if (sqlServerOptions.DbContextType != null)
            {
                _dbContext = serviceProvider.GetService(sqlServerOptions.DbContextType) as DbContext;
            }

            _diagnosticProcessor = serviceProvider.GetRequiredService<DiagnosticProcessorObserver>();
        }

        protected override void AddToSent(CapPublishedMessage msg)
        {
            if (DbTransaction is NoopTransaction)
            {
                base.AddToSent(msg);
                return;
            }

            var dbTransaction = DbTransaction as IDbTransaction;
            if (dbTransaction == null)
            {
                if (DbTransaction is IDbContextTransaction dbContextTransaction)
                {
                    dbTransaction = dbContextTransaction.GetDbTransaction();
                }

                if (dbTransaction == null)
                {
                    throw new ArgumentNullException(nameof(DbTransaction));
                }
            }

            var transactionKey = ((SqlConnection)dbTransaction.Connection).ClientConnectionId;
            if (_diagnosticProcessor.BufferList.TryGetValue(transactionKey, out var list))
            {
                list.Add(msg);
            }
            else
            {
                var msgList = new List<CapPublishedMessage>(1) { msg };
                _diagnosticProcessor.BufferList.TryAdd(transactionKey, msgList);
            }
        }

        public override void Commit()
        {
            switch (DbTransaction)
            {
                case NoopTransaction _:
                    Flush();
                    break;
                case IDbTransaction dbTransaction:
                    dbTransaction.Commit();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    _dbContext?.SaveChanges();
                    dbContextTransaction.Commit();
                    break;
            }
        }

        public override void Rollback()
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Rollback();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    dbContextTransaction.Rollback();
                    break;
            }
        }

        public override void Dispose()
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Dispose();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    dbContextTransaction.Dispose();
                    break;
            }
            DbTransaction = null;
        }
    }

    public static class CapTransactionExtensions
    {
        public static ICapTransaction Begin(this ICapTransaction transaction,
            IDbTransaction dbTransaction, bool autoCommit = false)
        {
            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        public static ICapTransaction Begin(this ICapTransaction transaction,
            IDbContextTransaction dbTransaction, bool autoCommit = false)
        {
            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        /// <summary>
        /// Start the CAP transaction
        /// </summary>
        /// <param name="dbConnection">The <see cref="IDbConnection" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <returns>The <see cref="ICapTransaction" /> object.</returns>
        public static IDbTransaction BeginTransaction(this IDbConnection dbConnection,
            ICapPublisher publisher, bool autoCommit = false)
        {
            if (dbConnection.State == ConnectionState.Closed)
            {
                dbConnection.Open();
            }

            var dbTransaction = dbConnection.BeginTransaction();
            publisher.Transaction.Value = publisher.ServiceProvider.GetService<CapTransactionBase>();
            var capTransaction = publisher.Transaction.Value.Begin(dbTransaction, autoCommit);
            return (IDbTransaction)capTransaction.DbTransaction;
        }

        /// <summary>
        /// Start the CAP transaction
        /// </summary>
        /// <param name="database">The <see cref="DatabaseFacade" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <returns>The <see cref="IDbContextTransaction" /> of EF dbcontext transaction object.</returns>
        public static IDbContextTransaction BeginTransaction(this DatabaseFacade database,
            ICapPublisher publisher, bool autoCommit = false)
        {
            var trans = database.BeginTransaction();
            publisher.Transaction.Value = publisher.ServiceProvider.GetService<CapTransactionBase>();
            var capTrans = publisher.Transaction.Value.Begin(trans, autoCommit);
            return new CapEFDbTransaction(capTrans);
        }
    }
}
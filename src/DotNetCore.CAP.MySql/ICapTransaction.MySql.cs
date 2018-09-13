﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class MySqlCapTransaction : CapTransactionBase
    {
        public MySqlCapTransaction(IDispatcher dispatcher) : base(dispatcher)
        {
        }

        public override void Commit()
        {
            Debug.Assert(DbTransaction != null);

            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Commit();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    dbContextTransaction.Commit();
                    break;
            }

            Flush();
        }

        public override void Rollback()
        {
            Debug.Assert(DbTransaction != null);

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
            (DbTransaction as IDbTransaction)?.Dispose();
        }
    }

    public static class CapTransactionExtensions
    {
        public static ICapTransaction Begin(this ICapTransaction transaction,
            IDbContextTransaction dbTransaction, bool autoCommit = false)
        {
            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        public static ICapTransaction Begin(this ICapTransaction transaction,
            IDbTransaction dbTransaction, bool autoCommit = false)
        {
            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
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
            var capTrans = publisher.Transaction.Begin(trans, autoCommit);
            return new CapEFDbTransaction(capTrans);
        }

        /// <summary>
        /// Start the CAP transaction
        /// </summary>
        /// <param name="dbConnection">The <see cref="IDbConnection" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <returns>The <see cref="ICapTransaction" /> object.</returns>
        public static ICapTransaction BeginTransaction(this IDbConnection dbConnection,
            ICapPublisher publisher, bool autoCommit = false)
        {
            if (dbConnection.State == ConnectionState.Closed)
            {
                dbConnection.Open();
            }

            var dbTransaction = dbConnection.BeginTransaction();
            return publisher.Transaction.Begin(dbTransaction, autoCommit);
        }
    }
}
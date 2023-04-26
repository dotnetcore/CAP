// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class PostgreSqlCapTransaction : CapTransactionBase
    {
        public PostgreSqlCapTransaction(IDispatcher dispatcher) : base(dispatcher)
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

        public override async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(DbTransaction != null);

            switch (DbTransaction)
            {
                case DbTransaction dbTransaction:
                    await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case IDbContextTransaction dbContextTransaction:
                    await dbContextTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
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

        public override async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(DbTransaction != null);

            switch (DbTransaction)
            {
                case DbTransaction dbTransaction:
                    await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case IDbContextTransaction dbContextTransaction:
                    await dbContextTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        public override void Dispose()
        {
            (DbTransaction as IDisposable)?.Dispose();
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
        public static ICapTransaction BeginTransaction(this IDbConnection dbConnection,
            ICapPublisher publisher, bool autoCommit = false)
        {
            if (dbConnection.State == ConnectionState.Closed) dbConnection.Open();

            var dbTransaction = dbConnection.BeginTransaction();
            publisher.Transaction.Value = ActivatorUtilities.CreateInstance<PostgreSqlCapTransaction>(publisher.ServiceProvider);
            return publisher.Transaction.Value.Begin(dbTransaction, autoCommit);
        }

        /// <summary>
        /// Start the CAP transaction
        /// </summary>
        /// <param name="database">The <see cref="DatabaseFacade" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <returns>The <see cref="IDbContextTransaction" /> of EF DbContext transaction object.</returns>
        public static IDbContextTransaction BeginTransaction(this DatabaseFacade database,
            ICapPublisher publisher, bool autoCommit = false)
        {
            var trans = database.BeginTransaction();
            publisher.Transaction.Value = ActivatorUtilities.CreateInstance<PostgreSqlCapTransaction>(publisher.ServiceProvider);
            var capTrans = publisher.Transaction.Value.Begin(trans, autoCommit);
            return new CapEFDbTransaction(capTrans);
        }

        /// <summary>
        /// Start the CAP transaction
        /// </summary>
        /// <param name="database">The <see cref="DatabaseFacade" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="isolationLevel">The <see cref="IsolationLevel" /> to use</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <returns>The <see cref="IDbContextTransaction" /> of EF DbContext transaction object.</returns>
        public static IDbContextTransaction BeginTransaction(this DatabaseFacade database,
            IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false)
        {
            var trans = database.BeginTransaction(isolationLevel);
            publisher.Transaction.Value = ActivatorUtilities.CreateInstance<PostgreSqlCapTransaction>(publisher.ServiceProvider);
            var capTrans = publisher.Transaction.Value.Begin(trans, autoCommit);
            return new CapEFDbTransaction(capTrans);
        }

        /// <summary>
        /// Start the CAP transaction async
        /// </summary>
        /// <param name="database">The <see cref="DatabaseFacade" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>The <see cref="IDbContextTransaction" /> of EF DbContext transaction object.</returns>
        public static async Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade database,
            ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
        {
            var trans = await database.BeginTransactionAsync(cancellationToken);
            publisher.Transaction.Value = ActivatorUtilities.CreateInstance<PostgreSqlCapTransaction>(publisher.ServiceProvider);
            var capTrans = publisher.Transaction.Value.Begin(trans, autoCommit);
            return new CapEFDbTransaction(capTrans);
        }

        /// <summary>
        /// Start the CAP transaction async
        /// </summary>
        /// <param name="database">The <see cref="DatabaseFacade" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="isolationLevel">The <see cref="IsolationLevel" /> to use</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>The <see cref="IDbContextTransaction" /> of EF DbContext transaction object.</returns>
        public static async Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade database,
            IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
        {
            var trans = await database.BeginTransactionAsync(isolationLevel, cancellationToken);
            publisher.Transaction.Value = ActivatorUtilities.CreateInstance<PostgreSqlCapTransaction>(publisher.ServiceProvider);
            var capTrans = publisher.Transaction.Value.Begin(trans, autoCommit);
            return new CapEFDbTransaction(capTrans);
        }
    }
}
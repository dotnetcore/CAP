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
namespace DotNetCore.CAP;

public class MySqlCapTransaction : CapTransactionBase
{
    public MySqlCapTransaction(
        IDispatcher dispatcher) : base(dispatcher)
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

        await FlushAsync();
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
}

public static class CapTransactionExtensions
{
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
        return BeginTransaction(database, IsolationLevel.Unspecified, publisher, autoCommit);
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
        var dbTransaction = database.BeginTransaction(isolationLevel);
        publisher.Transaction = ActivatorUtilities.CreateInstance<MySqlCapTransaction>(publisher.ServiceProvider);
        publisher.Transaction.DbTransaction = dbTransaction;
        publisher.Transaction.AutoCommit = autoCommit;
        return new CapEFDbTransaction(publisher.Transaction);
    }

    /// <summary>
    /// Start the CAP transaction async
    /// </summary>
    /// <param name="database">The <see cref="DatabaseFacade" />.</param>
    /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
    /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The <see cref="IDbContextTransaction" /> of EF DbContext transaction object.</returns>
    public static Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade database,
        ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
    {
        return BeginTransactionAsync(database, IsolationLevel.Unspecified, publisher, autoCommit, cancellationToken);
    }

    /// <summary>
    /// Start the CAP transaction async
    /// </summary>
    /// <param name="database">The <see cref="DatabaseFacade" />.</param>
    /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
    /// <param name="isolationLevel">The <see cref="IsolationLevel" /> to use</param>
    /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The <see cref="IDbContextTransaction" /> of EF DbContext transaction object.</returns>
    public static Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade database,
        IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
    {
        var dbTransaction = database.BeginTransactionAsync(isolationLevel, cancellationToken).GetAwaiter().GetResult();
        publisher.Transaction = ActivatorUtilities.CreateInstance<MySqlCapTransaction>(publisher.ServiceProvider);
        publisher.Transaction.DbTransaction = dbTransaction;
        publisher.Transaction.AutoCommit = autoCommit;
        return Task.FromResult<IDbContextTransaction>(new CapEFDbTransaction(publisher.Transaction));
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
        return BeginTransaction(dbConnection, IsolationLevel.Unspecified, publisher, autoCommit);
    }

    /// <summary>
    /// Start the CAP transaction
    /// </summary>
    /// <param name="dbConnection">The <see cref="IDbConnection" />.</param>
    /// <param name="isolationLevel">The <see cref="IsolationLevel" /> to use</param>
    /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
    /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
    /// <returns>The <see cref="ICapTransaction" /> object.</returns>
    public static ICapTransaction BeginTransaction(this IDbConnection dbConnection,
        IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false)
    {
        if (dbConnection.State == ConnectionState.Closed) dbConnection.Open();

        var dbTransaction = dbConnection.BeginTransaction(isolationLevel);
        publisher.Transaction = ActivatorUtilities.CreateInstance<MySqlCapTransaction>(publisher.ServiceProvider);
        publisher.Transaction.DbTransaction = dbTransaction;
        publisher.Transaction.AutoCommit = autoCommit;
        return publisher.Transaction;
    }

    /// <summary>
    /// Start the CAP transaction
    /// </summary>
    /// <param name="dbConnection">The <see cref="IDbConnection" />.</param>
    /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
    /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The <see cref="ICapTransaction" /> object.</returns>
    public static ValueTask<ICapTransaction> BeginTransactionAsync(this IDbConnection dbConnection,
        ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
    {
        return BeginTransactionAsync(dbConnection, IsolationLevel.Unspecified, publisher, autoCommit, cancellationToken);
    }

    /// <summary>
    /// Start the CAP transaction
    /// </summary>
    /// <param name="dbConnection">The <see cref="IDbConnection" />.</param>
    /// <param name="isolationLevel">The <see cref="IsolationLevel" /> to use</param>
    /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
    /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
    /// <param name="cancellationToken"></param>
    public static ValueTask<ICapTransaction> BeginTransactionAsync(this IDbConnection dbConnection,
        IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
    {
        if (dbConnection.State == ConnectionState.Closed) ((DbConnection)dbConnection).OpenAsync(cancellationToken).GetAwaiter().GetResult();

        var dbTransaction = ((DbConnection)dbConnection).BeginTransactionAsync(isolationLevel, cancellationToken).GetAwaiter().GetResult();
        publisher.Transaction = ActivatorUtilities.CreateInstance<MySqlCapTransaction>(publisher.ServiceProvider);
        publisher.Transaction.DbTransaction = dbTransaction;
        publisher.Transaction.AutoCommit = autoCommit;
        return ValueTask.FromResult(publisher.Transaction);
    }
}
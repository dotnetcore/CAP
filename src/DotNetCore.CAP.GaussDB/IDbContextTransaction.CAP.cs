using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// 把 CAP 事务适配成 EF Core 的 IDbContextTransaction，供 DatabaseFacade.BeginTransaction 返回。
/// </summary>
internal class CapEFDbTransaction : IDbContextTransaction, IInfrastructure<DbTransaction>
{
    private readonly ICapTransaction _transaction;

    public CapEFDbTransaction(ICapTransaction transaction)
    {
        _transaction = transaction;
        var dbContextTransaction = (IDbContextTransaction)_transaction.DbTransaction!;
        TransactionId = dbContextTransaction.TransactionId;
    }

    public void Dispose()
    {
        _transaction.Dispose();
    }

    public void Commit()
    {
        _transaction.Commit();
    }

    public void Rollback()
    {
        _transaction.Rollback();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.RollbackAsync(cancellationToken);
    }

    public Guid TransactionId { get; }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(Task.Run(() => _transaction.Dispose()));
    }

    /// <summary>
    /// 暴露 EF Core 底层 DbTransaction，满足 IInfrastructure 约定。
    /// </summary>
    public DbTransaction Instance
    {
        get
        {
            var dbContextTransaction = (IDbContextTransaction)_transaction.DbTransaction!;
            return dbContextTransaction.GetDbTransaction();
        }
    }
}
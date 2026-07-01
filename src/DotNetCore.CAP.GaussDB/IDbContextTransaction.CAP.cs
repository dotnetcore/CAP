using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Storage
{
    /// <summary>
    /// 把 CAP 事务适配成 EF Core 的 IDbContextTransaction，供 DatabaseFacade.BeginTransaction 返回。
    /// </summary>
    internal class CapEFDbTransaction : IDbContextTransaction, IInfrastructure<DbTransaction>
    {
        private readonly ICapTransaction _transaction;

        /// <summary>
        /// 将 CAP 事务包装为 EF Core 可识别的 <see cref="IDbContextTransaction"/>。
        /// </summary>
        /// <param name="transaction">内部的 CAP 事务实例。</param>
        public CapEFDbTransaction(ICapTransaction transaction)
        {
            _transaction = transaction;
            var dbContextTransaction = (IDbContextTransaction)_transaction.DbTransaction!;
            TransactionId = dbContextTransaction.TransactionId;
        }

        /// <summary>
        /// 释放底层 CAP 事务资源。
        /// </summary>
        public void Dispose()
        {
            _transaction.Dispose();
        }

        /// <inheritdoc />
        public void Commit()
        {
            _transaction.Commit();
        }

        /// <inheritdoc />
        public void Rollback()
        {
            _transaction.Rollback();
        }

        /// <inheritdoc />
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return _transaction.CommitAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return _transaction.RollbackAsync(cancellationToken);
        }

        /// <summary>
        /// 事务标识符。
        /// </summary>
        public Guid TransactionId { get; }

        /// <inheritdoc />
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
}
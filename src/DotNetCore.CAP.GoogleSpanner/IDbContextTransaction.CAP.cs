using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    internal class CapEFDbTransaction : IDbContextTransaction
    {
        private readonly ICapTransaction _transaction;

        public CapEFDbTransaction(ICapTransaction transaction)
        {
            _transaction = transaction;
            var dbContextTransaction = _transaction.DbTransaction as IDbContextTransaction;
            if (dbContextTransaction is not null)
            {
                TransactionId = dbContextTransaction.TransactionId;
            }
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
    }
}

// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Storage
{
    // ReSharper disable once InconsistentNaming
    internal class CapEFDbTransaction : IDbContextTransaction
    {
        private readonly ICapTransaction _transaction;

        public CapEFDbTransaction(ICapTransaction transaction)
        {
            _transaction = transaction;
            var dbContextTransaction = (IDbContextTransaction)_transaction.DbTransaction;
            TransactionId = dbContextTransaction.TransactionId;
        }

        public Guid TransactionId { get; }

        public void Dispose()
        {
            _transaction.Dispose();
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return _transaction.CommitAsync(cancellationToken);
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return _transaction.RollbackAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask();
        }
    }
}

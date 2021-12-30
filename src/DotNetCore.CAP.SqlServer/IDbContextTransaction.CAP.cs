// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Storage
{
    internal class CapEFDbTransaction : IDbContextTransaction, IInfrastructure<DbTransaction>
    {
        private readonly ICapTransaction _transaction;

        public CapEFDbTransaction(ICapTransaction transaction)
        {
            _transaction = transaction;
            var dbContextTransaction = (IDbContextTransaction)_transaction.DbTransaction!;
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

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.Run(() => _transaction.Dispose()));
        }

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
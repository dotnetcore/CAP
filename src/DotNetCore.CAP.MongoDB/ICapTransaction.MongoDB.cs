// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class MongoDBCapTransaction : CapTransactionBase
    {
        public MongoDBCapTransaction(IDispatcher dispatcher)
            : base(dispatcher)
        {
        }

        public override void Commit()
        {
            Debug.Assert(DbTransaction != null);

            if (DbTransaction is IClientSessionHandle session) session.CommitTransaction();

            Flush();
        }

        public override async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(DbTransaction != null);

            if (DbTransaction is IClientSessionHandle session) await session.CommitTransactionAsync(cancellationToken);

            Flush();
        }

        public override void Rollback()
        {
            Debug.Assert(DbTransaction != null);

            if (DbTransaction is IClientSessionHandle session) session.AbortTransaction();
        }

        public override async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(DbTransaction != null);

            if (DbTransaction is IClientSessionHandle session) await session.AbortTransactionAsync(cancellationToken);
        }

        public override void Dispose()
        {
            (DbTransaction as IClientSessionHandle)?.Dispose();
            DbTransaction = null;
        }
    }

    public static class CapTransactionExtensions
    {
        public static ICapTransaction Begin(this ICapTransaction transaction,
            IClientSessionHandle dbTransaction, bool autoCommit = false)
        {
            if (!dbTransaction.IsInTransaction) dbTransaction.StartTransaction();

            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        /// <summary>
        /// Start the CAP transaction
        /// </summary>
        /// <param name="client">The <see cref="IMongoClient" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <returns>The <see cref="IClientSessionHandle" /> of MongoDB transaction session object.</returns>
        public static IClientSessionHandle StartTransaction(this IMongoClient client,
            ICapPublisher publisher, bool autoCommit = false)
        {
            var clientSessionHandle = client.StartSession();
            publisher.Transaction.Value = ActivatorUtilities.CreateInstance<MongoDBCapTransaction>(publisher.ServiceProvider);
            var capTrans = publisher.Transaction.Value.Begin(clientSessionHandle, autoCommit);
            return new CapMongoDbClientSessionHandle(capTrans);
        }
    }
}
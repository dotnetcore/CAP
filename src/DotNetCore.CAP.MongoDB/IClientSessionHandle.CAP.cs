// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;

// ReSharper disable once CheckNamespace
namespace MongoDB.Driver
{
    internal class CapMongoDbClientSessionHandle : IClientSessionHandle
    {
        private readonly IClientSessionHandle _sessionHandle;
        private readonly ICapTransaction _transaction;

        public CapMongoDbClientSessionHandle(ICapTransaction transaction)
        {
            _transaction = transaction;
            _sessionHandle = (IClientSessionHandle)_transaction.DbTransaction!;
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }

        public void AbortTransaction(CancellationToken cancellationToken = default)
        {
            _transaction.Rollback();
        }

        public Task AbortTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction.Rollback();
            return Task.CompletedTask;
        }

        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            _sessionHandle.AdvanceClusterTime(newClusterTime);
        }

        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
            _sessionHandle.AdvanceOperationTime(newOperationTime);
        }

        public void CommitTransaction(CancellationToken cancellationToken = default)
        {
            _transaction.Commit();
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction.Commit();
            return Task.CompletedTask;
        }

        public void StartTransaction(TransactionOptions? transactionOptions = null)
        {
            _sessionHandle.StartTransaction(transactionOptions);
        }

        public IMongoClient Client => _sessionHandle.Client;
        public BsonDocument ClusterTime => _sessionHandle.ClusterTime;
        public bool IsImplicit => _sessionHandle.IsImplicit;
        public bool IsInTransaction => _sessionHandle.IsInTransaction;
        public BsonTimestamp OperationTime => _sessionHandle.OperationTime;
        public ClientSessionOptions Options => _sessionHandle.Options;
        public IServerSession ServerSession => _sessionHandle.ServerSession;
        public ICoreSessionHandle WrappedCoreSession => _sessionHandle.WrappedCoreSession;

        public IClientSessionHandle Fork()
        {
            return _sessionHandle.Fork();
        }

        public TResult WithTransaction<TResult>(Func<IClientSessionHandle, CancellationToken, TResult> callback, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default)
        {
            return _sessionHandle.WithTransaction(callback, transactionOptions, cancellationToken);
        }

        public Task<TResult> WithTransactionAsync<TResult>(Func<IClientSessionHandle, CancellationToken, Task<TResult>> callbackAsync, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default)
        {
            return _sessionHandle.WithTransactionAsync(callbackAsync, transactionOptions, cancellationToken);
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;

namespace DotNetCore.CAP
{
    public abstract class CapTransactionBase : ICapTransaction
    {
        private readonly IDispatcher _dispatcher;

        private readonly ConcurrentQueue<IMediumMessage> _bufferList;

        protected CapTransactionBase(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _bufferList = new ConcurrentQueue<IMediumMessage>();
        }

        public bool AutoCommit { get; set; }

        public object DbTransaction { get; set; }

        protected internal virtual void AddToSent(IMediumMessage msg)
        {
            _bufferList.Enqueue(msg);
        }

        protected virtual void Flush()
        {
            while (!_bufferList.IsEmpty)
            {
                _bufferList.TryDequeue(out var message);

                _dispatcher.EnqueueToPublish(message);
            }
        }

        public abstract void Commit();

        public abstract Task CommitAsync(CancellationToken cancellationToken = default);

        public abstract void Rollback();

        public abstract Task RollbackAsync(CancellationToken cancellationToken = default);

        public abstract void Dispose();
    }
}

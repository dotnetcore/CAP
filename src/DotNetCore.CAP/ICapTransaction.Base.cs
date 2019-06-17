using System.Collections.Concurrent;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public abstract class CapTransactionBase : ICapTransaction
    {
        private readonly IDispatcher _dispatcher;

        private readonly ConcurrentQueue<CapPublishedMessage> _bufferList;

        protected CapTransactionBase(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _bufferList = new ConcurrentQueue<CapPublishedMessage>();
        }

        public bool AutoCommit { get; set; }

        public object DbTransaction { get; set; }

        protected internal virtual void AddToSent(CapPublishedMessage msg)
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

        public abstract void Rollback();

        public abstract void Dispose();
    }
}

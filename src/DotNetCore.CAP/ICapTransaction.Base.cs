using System.Collections.Generic;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public abstract class CapTransactionBase : ICapTransaction
    {
        private readonly IDispatcher _dispatcher;

        private readonly IList<CapPublishedMessage> _bufferList;

        protected CapTransactionBase(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _bufferList = new List<CapPublishedMessage>(1);
        }

        public bool AutoCommit { get; set; }

        public object DbTransaction { get; set; }

        protected internal virtual void AddToSent(CapPublishedMessage msg)
        {
            _bufferList.Add(msg);
        }

        protected virtual void Flush()
        {
            foreach (var message in _bufferList)
            {
                _dispatcher.EnqueueToPublish(message);
            }

            _bufferList.Clear();
        }

        public abstract void Commit();

        public abstract void Rollback();

        public abstract void Dispose();
    }
}

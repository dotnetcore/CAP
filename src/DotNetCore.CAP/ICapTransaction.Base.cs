// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;

namespace DotNetCore.CAP;

public abstract class CapTransactionBase : ICapTransaction
{
    private readonly ConcurrentQueue<MediumMessage> _bufferList;
    private readonly IDispatcher _dispatcher;

    protected CapTransactionBase(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _bufferList = new ConcurrentQueue<MediumMessage>();
    }

    public bool AutoCommit { get; set; }

    public virtual object? DbTransaction { get; set; }

    public abstract void Commit();

    public abstract Task CommitAsync(CancellationToken cancellationToken = default);

    public abstract void Rollback();

    public abstract Task RollbackAsync(CancellationToken cancellationToken = default);

    public abstract void Dispose();

    protected internal virtual void AddToSent(MediumMessage msg)
    {
        _bufferList.Enqueue(msg);
    }

    protected virtual void Flush()
    {
        while (!_bufferList.IsEmpty)
        {
            if (_bufferList.TryDequeue(out var message))
            {
                var isDelayMessage = message.Origin.Headers.ContainsKey(Headers.DelayTime);
                if (isDelayMessage)
                {
                    _dispatcher.EnqueueToScheduler(message, DateTime.Parse(message.Origin.Headers[Headers.SentTime]!)).ConfigureAwait(false);
                }
                else
                {
                    _dispatcher.EnqueueToPublish(message).ConfigureAwait(false);
                }
            }
        }
    }
}
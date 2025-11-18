// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;

namespace DotNetCore.CAP;

/// <summary>
/// A thread-safe holder for storing the current CAP transaction context within a scope (e.g., per HTTP request or async execution context).
/// This is used internally to associate a transaction with the ambient execution context.
/// </summary>
internal sealed class CapTransactionHolder
{
    /// <summary>
    /// Gets or sets the CAP transaction associated with the current context.
    /// </summary>
    public ICapTransaction? Transaction;
}

/// <summary>
/// Provides a base implementation of <see cref="ICapTransaction"/> that manages message publishing within a database transaction.
/// This class handles buffering, flushing, and coordination of messages with the message transport layer.
/// </summary>
/// <remarks>
/// This base class:
/// <list type="bullet">
/// <item><description>Maintains an internal queue of messages to be published.</description></item>
/// <item><description>Provides methods to add messages to the queue and flush them to the dispatcher.</description></item>
/// <item><description>Handles both delayed and immediate message publishing based on message headers.</description></item>
/// <item><description>Integrates with the dispatcher to enqueue messages for publishing or scheduling.</description></item>
/// </list>
/// Derived classes must implement the transaction-specific Commit/Rollback operations.
/// </remarks>
public abstract class CapTransactionBase : ICapTransaction
{
    private readonly ConcurrentQueue<MediumMessage> _bufferList;
    private readonly IDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapTransactionBase"/> class with a dispatcher.
    /// </summary>
    /// <param name="dispatcher">The dispatcher used to enqueue messages for publishing and execution.</param>
    protected CapTransactionBase(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _bufferList = new ConcurrentQueue<MediumMessage>();
    }

    /// <summary>
    /// Gets or sets a value indicating whether this transaction is automatically committed after a message is published.
    /// When true, the transaction commits immediately; when false, manual commit is required.
    /// </summary>
    public bool AutoCommit { get; set; }

    /// <summary>
    /// Gets or sets the underlying database transaction object.
    /// This can be cast to the specific database transaction type (e.g., SqlTransaction, NpgsqlTransaction) when needed.
    /// </summary>
    public virtual object? DbTransaction { get; set; }

    /// <summary>
    /// Commits the transaction synchronously, causing all buffered messages to be sent to the message queue.
    /// </summary>
    public abstract void Commit();

    /// <summary>
    /// Asynchronously commits the transaction, causing all buffered messages to be sent to the message queue.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous commit operation.</returns>
    public abstract Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction synchronously, discarding all buffered messages without sending them.
    /// </summary>
    public abstract void Rollback();

    /// <summary>
    /// Asynchronously rolls back the transaction, discarding all buffered messages without sending them.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    public abstract Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message to the internal buffer queue to be sent when the transaction is committed.
    /// This is typically called when publishing a message within a transaction context.
    /// </summary>
    /// <param name="msg">The message to add to the buffer.</param>
    protected internal virtual void AddToSent(MediumMessage msg)
    {
        _bufferList.Enqueue(msg);
    }

    /// <summary>
    /// Synchronously flushes all buffered messages from the internal queue to the dispatcher.
    /// This method blocks until all messages have been enqueued.
    /// </summary>
    protected virtual void Flush()
    {
        FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously flushes all buffered messages from the internal queue to the dispatcher.
    /// Delayed messages are enqueued to the scheduler; immediate messages are enqueued for publishing.
    /// </summary>
    /// <returns>A task representing the asynchronous flush operation.</returns>
    protected virtual async Task FlushAsync()
    {
        while (!_bufferList.IsEmpty)
        {
            if (_bufferList.TryDequeue(out var message))
            {
                var isDelayMessage = message.Origin.Headers.ContainsKey(Headers.DelayTime);
                if (isDelayMessage)
                {
                    await _dispatcher.EnqueueToScheduler(message, DateTime.Parse(message.Origin.Headers[Headers.SentTime]!, CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }
                else
                {
                    await _dispatcher.EnqueueToPublish(message).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Disposes the transaction, releasing the underlying database transaction if it implements <see cref="IDisposable"/>.
    /// </summary>
    public virtual void Dispose()
    {
        (DbTransaction as IDisposable)?.Dispose();
        DbTransaction = null;
    }
}
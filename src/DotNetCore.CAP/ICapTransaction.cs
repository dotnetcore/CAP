// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP;

/// <summary>
/// Represents a CAP transaction wrapper that coordinates message publishing with a database transaction.
/// This interface provides a consistent API for managing outbox pattern implementations where messages
/// must be published atomically with database changes.
/// </summary>
/// <remarks>
/// The CAP transaction wrapper enables the reliable message delivery pattern by ensuring that:
/// <list type="bullet">
/// <item><description>Messages are published only when the database transaction succeeds.</description></item>
/// <item><description>Uncommitted messages are discarded if the transaction is rolled back.</description></item>
/// <item><description>Different message brokers and databases can be supported through implementation-specific subclasses.</description></item>
/// </list>
/// Applications typically obtain an instance of this interface through dependency injection and associate it
/// with a database transaction before publishing messages within that transaction.
/// </remarks>
public interface ICapTransaction : IDisposable
{
    /// <summary>
    /// Gets or sets a value indicating whether the transaction is automatically committed after a message is published.
    /// When true, the transaction commits immediately upon message publishing; when false, manual commit is required.
    /// </summary>
    bool AutoCommit { get; set; }

    /// <summary>
    /// Gets or sets the underlying database transaction object.
    /// Can be cast to specific database transaction types (e.g., SqlTransaction, NpgsqlTransaction, IDbTransaction)
    /// for database-specific operations.
    /// </summary>
    object? DbTransaction { get; set; }

    /// <summary>
    /// Synchronously commits the transaction context, causing all buffered CAP messages to be sent to the message queue.
    /// This must be called after publishing messages within a transaction to ensure they are delivered.
    /// </summary>
    void Commit();

    /// <summary>
    /// Asynchronously commits the transaction context, causing all buffered CAP messages to be sent to the message queue.
    /// This must be called after publishing messages within a transaction to ensure they are delivered.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous commit operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously rolls back the transaction context, discarding all buffered CAP messages without sending them.
    /// This cancels any messages that were queued but not yet committed.
    /// </summary>
    void Rollback();

    /// <summary>
    /// Asynchronously rolls back the transaction context, discarding all buffered CAP messages without sending them.
    /// This cancels any messages that were queued but not yet committed.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
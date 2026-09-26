// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams;

internal class RedisConnectionPool : IRedisConnectionPool, IDisposable
{
    private readonly ConcurrentBag<AsyncLazyRedisConnection> _connections = [];

    private readonly Func<ConfigurationOptions, TextWriter, Task<IConnectionMultiplexer>>? _connectionFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly CapRedisOptions _redisOptions;
    private readonly TimeSpan? _retryDelay;
    private int _disposed;

    public RedisConnectionPool(IOptions<CapRedisOptions> options, ILoggerFactory loggerFactory)
        : this(options, loggerFactory, connectionFactory: null, retryDelay: null)
    {
    }

    /// <summary>
    ///     Testing seam. Lets unit tests inject a fake multiplexer factory and a shorter
    ///     retry delay into every slot the pool creates; <c>null</c> means "use the real
    ///     defaults" (see <see cref="AsyncLazyRedisConnection" />). DI always resolves the
    ///     public constructor, so production behavior is unchanged.
    /// </summary>
    internal RedisConnectionPool(IOptions<CapRedisOptions> options, ILoggerFactory loggerFactory,
        Func<ConfigurationOptions, TextWriter, Task<IConnectionMultiplexer>>? connectionFactory,
        TimeSpan? retryDelay)
    {
        _redisOptions = options.Value;
        _loggerFactory = loggerFactory;
        _connectionFactory = connectionFactory;
        _retryDelay = retryDelay;
        Init();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<IConnectionMultiplexer> ConnectAsync()
    {
        if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(RedisConnectionPool));

        // Recompute pool state from healthy slots only. A slot whose connect task
        // faulted (e.g. sentinel failover) must NOT count as "configured", otherwise
        // the pool would consider itself full and keep handing out the poisoned slot.
        // Kept as a local — persisting it as a shared field caused a data race under
        // concurrent publish/consume.
        var poolConfigured =
            _connections.Count(static c => c.TryGetCreatedConnection(out _)) == _redisOptions.ConnectionPoolSize;

        if (poolConfigured)
        {
            var quiet = SelectLeastLoadedHealthy();
            if (quiet != null) return quiet.Connection;
        }

        foreach (var lazy in _connections)
        {
            // Uninitialized or poisoned slot → (re)connect it. A healthy in-flight
            // task is reused by GetAwaiter; a faulted/cancelled one is discarded and
            // the factory retried.
            if (!lazy.TryGetCreatedConnection(out var created)) return (await lazy).Connection;

            // Healthy connection with no outstanding operations → reuse it.
            if (created.ConnectionCapacity == default) return created.Connection;
        }

        // All slots are healthy and busy → pick the least-loaded one.
        var leastLoaded = SelectLeastLoadedHealthy();
        if (leastLoaded != null) return leastLoaded.Connection;

        // Unreachable with a non-empty pool: a successfully completed task is terminal,
        // so a slot the loop above saw as healthy cannot turn unhealthy before
        // SelectLeastLoadedHealthy runs. Kept as a defensive last resort; on an empty
        // pool First() throws, but PostConfigure guarantees at least one slot.
        return (await _connections.First()).Connection;
    }

    /// <summary>
    ///     Returns the healthy connection with the lowest outstanding capacity, skipping
    ///     any poisoned slot. Never throws — used by both the hot path and diagnostics.
    /// </summary>
    private RedisConnection? SelectLeastLoadedHealthy()
    {
        RedisConnection? best = null;
        var bestCapacity = long.MaxValue;

        foreach (var lazy in _connections)
        {
            if (!lazy.TryGetCreatedConnection(out var created)) continue;

            var capacity = created.ConnectionCapacity;
            if (capacity < bestCapacity)
            {
                bestCapacity = capacity;
                best = created;
            }
        }

        return best;
    }

    private void Init()
    {
        if (!_connections.IsEmpty) return;

        for (var i = 0; i < _redisOptions.ConnectionPoolSize; i++)
        {
            var connection = new AsyncLazyRedisConnection(_redisOptions,
                _loggerFactory.CreateLogger<AsyncLazyRedisConnection>(), _connectionFactory, _retryDelay);

            _connections.Add(connection);
        }
    }

    // Pool disposal is best-effort at shutdown: a ConnectAsync racing with Dispose may
    // leak a multiplexer or observe a disposed connection. For an in-flight caller that
    // already passed the disposed-check this is not just a leak but a use-after-dispose:
    // it can be handed a connection that the cleanup continuation below disposes right
    // away, so its next publish/consume fails with ObjectDisposedException. Accepted —
    // the process is terminating and the OS reclaims sockets; fully closing the window
    // would require locking the publish/consume hot path for the sake of a dying process.
    private void Dispose(bool disposing)
    {
        // Atomic check/set: a concurrent Dispose must not run the cleanup loop twice.
        // Publishes disposal before releasing connections so a concurrent ConnectAsync
        // observes it and does not create a fresh slot no one would ever close.
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        if (!disposing) return;

        foreach (var connection in _connections)
        {
            // Read the slot's task once, then dispose its connection through a single
            // continuation. ContinueWith runs synchronously and immediately when the task
            // is already complete, so already-connected, in-flight, and racing-completion
            // are all handled with ONE observation of the task's state — branching on two
            // separate reads (IsCompletedSuccessfully then IsCompleted) would let a task
            // that completes between the reads slip through undisposed. Faulted/cancelled
            // tasks produce no connection, so the success guard skips them. Don't block
            // shutdown waiting for an in-flight connect to finish.
            var pending = connection.PeekTask();
            if (pending is null) continue; // slot never initialized — nothing to dispose

            pending.ContinueWith(static t =>
                {
                    if (t.IsCompletedSuccessfully)
                        // Swallow failures so one slot's Dispose can't leave the rest leaked.
                        try
                        {
                            t.Result.Dispose();
                        }
                        catch
                        {
                            /* best-effort cleanup at shutdown */
                        }
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}

// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams;

public class AsyncLazyRedisConnection
{
    internal const int MaxConnectAttempts = 5;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

    private readonly Func<ConfigurationOptions, TextWriter, Task<IConnectionMultiplexer>> _connectionFactory;
    private readonly ILogger<AsyncLazyRedisConnection> _logger;
    private readonly CapRedisOptions _redisOptions;
    private readonly TimeSpan _retryDelay;

    // Guards creation/replacement of the cached connect task. The previous
    // implementation inherited from Lazy<Task<RedisConnection>>, which caches a
    // faulted task forever: once the sentinel connect path threw, the pool never
    // recovered after a Redis failover. We keep the task ourselves so a faulted or
    // cancelled task can be discarded and the factory retried, while a healthy or
    // still in-flight task is reused.
    private readonly object _lock = new();
    private Task<RedisConnection>? _task;

    public AsyncLazyRedisConnection(
        CapRedisOptions redisOptions,
        ILogger<AsyncLazyRedisConnection> logger)
        : this(redisOptions, logger, connectionFactory: null, retryDelay: null)
    {
    }

    /// <summary>
    ///     Testing seam. Lets unit tests substitute the multiplexer factory (so the
    ///     retry/dispose/recovery logic can run against a fake instead of a real network
    ///     connect) and shorten the delay between attempts. Production code always goes
    ///     through the public constructor, which keeps the real
    ///     <see cref="ConnectionMultiplexer.ConnectAsync(ConfigurationOptions, TextWriter)" />
    ///     factory and the default retry delay.
    /// </summary>
    internal AsyncLazyRedisConnection(
        CapRedisOptions redisOptions,
        ILogger<AsyncLazyRedisConnection> logger,
        Func<ConfigurationOptions, TextWriter, Task<IConnectionMultiplexer>>? connectionFactory,
        TimeSpan? retryDelay)
    {
        _redisOptions = redisOptions;
        _logger = logger;
        _connectionFactory = connectionFactory ?? DefaultConnectionFactoryAsync;
        _retryDelay = retryDelay ?? DefaultRetryDelay;
    }

    /// <summary>
    ///     Non-blocking, non-throwing accessor for the created connection. Returns
    ///     <c>false</c> for a slot that has not been created, is still connecting, or
    ///     whose connect task faulted or was cancelled (a "poisoned" slot).
    /// </summary>
    public bool TryGetCreatedConnection([NotNullWhen(true)] out RedisConnection? connection)
    {
        Task<RedisConnection>? task;
        lock (_lock)
        {
            task = _task;
        }

        if (task is { IsCompletedSuccessfully: true })
        {
            connection = task.Result;
            return true;
        }

        connection = null;
        return false;
    }

    /// <summary>
    ///     Returns the current connect task without creating or replacing it. Unlike
    ///     <see cref="TryGetCreatedConnection" /> this exposes an in-flight or poisoned
    ///     task too, so pool disposal can attach cleanup to a connection that is still
    ///     being established without forcing a reconnect.
    /// </summary>
    public Task<RedisConnection>? PeekTask()
    {
        lock (_lock)
        {
            return _task;
        }
    }

    public TaskAwaiter<RedisConnection> GetAwaiter()
    {
        return GetOrCreateTask().GetAwaiter();
    }

    /// <summary>
    ///     Returns the cached connect task, (re)creating it when the slot has never been
    ///     initialized or when the previous task faulted or was cancelled. A healthy or
    ///     still in-flight task is reused. Racing callers are serialized by the lock, so
    ///     the factory is invoked at most once per reset.
    /// </summary>
    private Task<RedisConnection> GetOrCreateTask()
    {
        lock (_lock)
        {
            if (_task is null || _task.IsFaulted || _task.IsCanceled)
                _task = ConnectAsync();

            return _task;
        }
    }

    private static async Task<IConnectionMultiplexer> DefaultConnectionFactoryAsync(
        ConfigurationOptions configuration, TextWriter log)
    {
        return await ConnectionMultiplexer.ConnectAsync(configuration, log).ConfigureAwait(false);
    }

    private async Task<RedisConnection> ConnectAsync()
    {
        var redisLogger = new RedisLogger(_logger);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxConnectAttempts; attempt++)
        {
            IConnectionMultiplexer? connection = null;
            try
            {
                connection = await _connectionFactory(_redisOptions.Configuration!, redisLogger)
                    .ConfigureAwait(false);

                connection.LogEvents(_logger);

                if (connection.IsConnected)
                    return new RedisConnection(connection);

                // AbortOnConnectFail=false returns a DISCONNECTED multiplexer instead of
                // throwing. Treat it as a failed attempt so we never cache a dead connection.
                lastException = new InvalidOperationException($"Redis connection is not connected [attempt {attempt}].");
            }
            catch (Exception ex)
            {
                // AbortOnConnectFail=true makes ConnectAsync throw once connect fails.
                lastException = ex;
            }

            // Failed attempt: dispose the multiplexer (if one was created) so its sockets
            // and background reconnect loop don't leak, then retry after a delay. This runs
            // on both the throw path and the disconnected-multiplexer path. Swallow a
            // failing Dispose so it can't abort the remaining retry attempts.
            try
            {
                connection?.Dispose();
            }
            catch
            {
                /* best-effort cleanup between retries */
            }

            // Only advertise a follow-up attempt when one will actually happen. On the
            // final failure we stay silent here; the method then throws with a summarizing
            // message so we don't promise a retry that never comes.
            if (attempt < MaxConnectAttempts)
            {
                _logger.LogWarning(lastException,
                    "Can't establish redis connection, trying to establish connection [attempt {attempt}].", attempt);

                await Task.Delay(_retryDelay).ConfigureAwait(false);
            }
        }

        // All attempts exhausted without a live connection. Throw so the connect task
        // faults and GetOrCreateTask recreates the slot on the next access, instead of
        // caching a dead/disconnected multiplexer forever.
        throw new InvalidOperationException(
            $"Can't establish redis connection after [{MaxConnectAttempts}] attempts.", lastException);
    }
}

public class RedisConnection(IConnectionMultiplexer connection) : IDisposable
{
    private bool _isDisposed;
    public IConnectionMultiplexer Connection { get; } = connection ?? throw new ArgumentNullException(nameof(connection));
    public long ConnectionCapacity => Connection.GetCounters().TotalOutstanding;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing) Connection.Dispose();

        _isDisposed = true;
    }
}

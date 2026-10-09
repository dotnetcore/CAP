// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace DotNetCore.CAP.RedisStreams.Test;

/// <summary>
///     Covers the connection pool recovery fix: a slot whose connect attempt failed
///     (threw, or produced a disconnected multiplexer) must not be cached forever.
///     The pool has to retry the factory and hand out a live connection once Redis
///     is reachable again, and every failed-attempt multiplexer must be disposed.
/// </summary>
public class RedisConnectionPoolTest
{
    [Fact]
    public async Task ConnectAsync_recovers_after_redis_comes_back()
    {
        // First run: every attempt throws (e.g. sentinel failover while the master is
        // re-elected). Second run: Redis is back and the factory returns a connected
        // multiplexer.
        var connected = CreateMultiplexer(isConnected: true);
        var factoryCalls = 0;

        Task<IConnectionMultiplexer> Factory(ConfigurationOptions configuration, TextWriter log)
        {
            factoryCalls++;
            if (factoryCalls <= AsyncLazyRedisConnection.MaxConnectAttempts)
                return Task.FromException<IConnectionMultiplexer>(
                    new RedisConnectionException(ConnectionFailureType.UnableToConnect, "sentinel failover"));

            return Task.FromResult(connected);
        }

        using var pool = CreatePool(Factory);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => pool.ConnectAsync());
        Assert.IsType<RedisConnectionException>(thrown.InnerException);
        Assert.Equal(AsyncLazyRedisConnection.MaxConnectAttempts, factoryCalls);

        // The old Lazy<Task<...>>-based slot cached the faulted task forever, so this
        // second call used to rethrow the stale exception instead of reconnecting.
        var result = await pool.ConnectAsync();

        Assert.Same(connected, result);
        Assert.Equal(AsyncLazyRedisConnection.MaxConnectAttempts + 1, factoryCalls);
    }

    [Fact]
    public async Task ConnectAsync_does_not_cache_disconnected_multiplexer_as_healthy()
    {
        // AbortOnConnectFail=false makes ConnectionMultiplexer.ConnectAsync return a
        // DISCONNECTED multiplexer instead of throwing. The pool must treat it as a
        // failed attempt, keep calling the factory on later requests, and never hand
        // the dead multiplexer out.
        var disconnected = new List<IConnectionMultiplexer>();
        var connected = CreateMultiplexer(isConnected: true);
        var redisIsUp = false;

        Task<IConnectionMultiplexer> Factory(ConfigurationOptions configuration, TextWriter log)
        {
            if (redisIsUp) return Task.FromResult(connected);

            var multiplexer = CreateMultiplexer(isConnected: false);
            disconnected.Add(multiplexer);
            return Task.FromResult(multiplexer);
        }

        using var pool = CreatePool(Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pool.ConnectAsync());
        Assert.Equal(AsyncLazyRedisConnection.MaxConnectAttempts, disconnected.Count);

        // The slot is poisoned, not healthy: the next request runs the factory again
        // (a full second round of attempts) instead of returning a dead connection.
        await Assert.ThrowsAsync<InvalidOperationException>(() => pool.ConnectAsync());
        Assert.Equal(2 * AsyncLazyRedisConnection.MaxConnectAttempts, disconnected.Count);

        redisIsUp = true;
        var result = await pool.ConnectAsync();

        Assert.Same(connected, result);
    }

    [Fact]
    public async Task ConnectAsync_disposes_every_failed_attempt_multiplexer()
    {
        // Each failed attempt creates a multiplexer with live sockets and a background
        // reconnect loop; leaving them undisposed leaks both. Every one of the
        // MaxConnectAttempts multiplexers must be disposed exactly once.
        var disconnected = new List<IConnectionMultiplexer>();

        Task<IConnectionMultiplexer> Factory(ConfigurationOptions configuration, TextWriter log)
        {
            var multiplexer = CreateMultiplexer(isConnected: false);
            disconnected.Add(multiplexer);
            return Task.FromResult(multiplexer);
        }

        using var pool = CreatePool(Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pool.ConnectAsync());

        Assert.Equal(AsyncLazyRedisConnection.MaxConnectAttempts, disconnected.Count);
        foreach (var multiplexer in disconnected)
            multiplexer.Received(1).Dispose();
    }

    [Fact]
    public async Task ConnectAsync_returns_connected_multiplexer_on_first_attempt()
    {
        var connected = CreateMultiplexer(isConnected: true);
        var factoryCalls = 0;

        Task<IConnectionMultiplexer> Factory(ConfigurationOptions configuration, TextWriter log)
        {
            factoryCalls++;
            return Task.FromResult(connected);
        }

        using var pool = CreatePool(Factory);

        var result = await pool.ConnectAsync();

        Assert.Same(connected, result);
        Assert.Equal(1, factoryCalls);
        connected.DidNotReceive().Dispose();

        // A healthy cached slot is reused without another factory run.
        var again = await pool.ConnectAsync();

        Assert.Same(connected, again);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task Dispose_disposes_created_connections()
    {
        var connected = CreateMultiplexer(isConnected: true);
        var pool = CreatePool((_, _) => Task.FromResult(connected));

        await pool.ConnectAsync();

        pool.Dispose();

        connected.Received(1).Dispose();
    }

    [Fact]
    public async Task ConnectAsync_throws_ObjectDisposedException_after_dispose()
    {
        var pool = CreatePool((_, _) => Task.FromResult(CreateMultiplexer(isConnected: true)));
        pool.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => pool.ConnectAsync());
    }

    [Fact]
    public async Task ConnectAsync_poisoned_slot_does_not_prevent_returning_healthy_connection()
    {
        // Core of the bug this fork fixes. In the old Lazy<Task<...>>-based pool the
        // CreatedConnection getter did Value.GetAwaiter().GetResult(), so a slot whose
        // connect task had faulted RETHREW that cached exception from every selection
        // path (QuietConnection's OrderBy over all slots and the foreach capacity
        // probe). One poisoned slot therefore took ConnectAsync down for the WHOLE
        // pool - publish and consume both failed - even while another slot held a
        // perfectly healthy connection, and the Lazy slot never retried. This test
        // drives a two-slot pool into the "one slot poisoned" state and proves later
        // requests still get a live connection instead of the stale exception.
        var connected = CreateMultiplexer(isConnected: true);
        var factoryCalls = 0;

        Task<IConnectionMultiplexer> Factory(ConfigurationOptions configuration, TextWriter log)
        {
            factoryCalls++;
            // Exactly one full retry round fails, poisoning the first slot the pool
            // touches; every later call yields a healthy multiplexer.
            if (factoryCalls <= AsyncLazyRedisConnection.MaxConnectAttempts)
                return Task.FromException<IConnectionMultiplexer>(
                    new RedisConnectionException(ConnectionFailureType.UnableToConnect, "node down"));

            return Task.FromResult(connected);
        }

        using var pool = CreatePool(Factory, poolSize: 2);

        // Poison one slot: the first request exhausts all attempts on it and throws.
        await Assert.ThrowsAsync<InvalidOperationException>(() => pool.ConnectAsync());
        Assert.Equal(AsyncLazyRedisConnection.MaxConnectAttempts, factoryCalls);

        // A poisoned slot now sits in the pool. The next request must NOT rethrow its
        // cached failure: the pool (re)connects a non-healthy slot with the now-working
        // factory - exactly one more run - and hands out the live multiplexer.
        var result = await pool.ConnectAsync();

        Assert.Same(connected, result);
        Assert.Equal(AsyncLazyRedisConnection.MaxConnectAttempts + 1, factoryCalls);

        // Depending on ConcurrentBag enumeration order the call above healed either
        // the poisoned slot (leaving the other uninitialized) or the untouched slot
        // (leaving the poison in place). Both states mix one healthy slot with one
        // non-healthy slot, and the next request must again return a live connection
        // without throwing: it either reuses the healthy slot (no factory run) or
        // repairs the broken one (one factory run).
        var again = await pool.ConnectAsync();

        Assert.Same(connected, again);
        Assert.InRange(factoryCalls,
            AsyncLazyRedisConnection.MaxConnectAttempts + 1,
            AsyncLazyRedisConnection.MaxConnectAttempts + 2);
    }

    [Fact]
    public async Task TryGetCreatedConnection_does_not_throw_for_poisoned_slot()
    {
        // Slot-level half of the poisoned-slot invariant: the accessor the pool's
        // selection paths rely on (the poolConfigured count and
        // SelectLeastLoadedHealthy both call TryGetCreatedConnection) must stay
        // non-throwing for a faulted slot so selection can skip it. The old
        // equivalent, CreatedConnection => Value.GetAwaiter().GetResult(), rethrew
        // the cached connect exception - that is what made one poisoned slot fatal
        // pool-wide.
        var poisonedSlot = CreateSlot((_, _) => Task.FromException<IConnectionMultiplexer>(
            new RedisConnectionException(ConnectionFailureType.UnableToConnect, "node down")));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await poisonedSlot);

        // Non-throwing "no usable connection" answer lets selection skip the slot.
        Assert.False(poisonedSlot.TryGetCreatedConnection(out var fromPoisoned));
        Assert.Null(fromPoisoned);
        // The faulted task itself stays observable (pool disposal peeks at it).
        Assert.True(poisonedSlot.PeekTask()!.IsFaulted);

        var connected = CreateMultiplexer(isConnected: true);
        var healthySlot = CreateSlot((_, _) => Task.FromResult(connected));
        var healthy = await healthySlot;

        Assert.True(healthySlot.TryGetCreatedConnection(out var fromHealthy));
        Assert.Same(healthy, fromHealthy);
        Assert.Same(connected, fromHealthy!.Connection);
    }

    [Fact]
    public async Task Dispose_disposes_multiplexer_from_connect_still_in_flight()
    {
        // Disposing the pool while a slot's connect attempt is still running must not
        // leak the connection that attempt eventually produces: Dispose attaches a
        // continuation to the in-flight task, and once the factory completes the
        // produced multiplexer gets disposed. The factory is gated on a
        // TaskCompletionSource so "in flight" is a guaranteed state, not a timing.
        var connected = CreateMultiplexer(isConnected: true);
        var gate = new TaskCompletionSource<IConnectionMultiplexer>();

        var pool = CreatePool((_, _) => gate.Task);

        var connectTask = pool.ConnectAsync();
        Assert.False(connectTask.IsCompleted); // connect is gated: still in flight

        pool.Dispose();
        connected.DidNotReceive().Dispose(); // nothing produced yet - nothing to dispose

        // Redis "answers" after shutdown started: the continuation registered by
        // Dispose must clean the late connection up.
        gate.SetResult(connected);

        // The request that had already passed the disposed-check still completes;
        // pool disposal is documented as best-effort towards in-flight callers.
        var result = await connectTask;
        Assert.Same(connected, result);

        connected.Received(1).Dispose();
    }

    [Fact]
    public async Task Dispose_called_twice_disposes_connection_only_once()
    {
        // Interlocked.Exchange in Dispose must make the cleanup run exactly once;
        // RedisConnection's own dispose-guard is the second belt. The user-visible
        // contract: double-disposing the pool neither throws nor disposes the
        // multiplexer a second time.
        var connected = CreateMultiplexer(isConnected: true);
        var pool = CreatePool((_, _) => Task.FromResult(connected));

        await pool.ConnectAsync();

        pool.Dispose();
        pool.Dispose();

        connected.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_with_never_connected_slot_does_not_connect_or_throw()
    {
        // A slot that was never asked for a connection has no task (PeekTask() is
        // null). Disposing the pool must skip it silently - in particular it must
        // not force a connect just to have something to dispose.
        var factoryCalls = 0;

        Task<IConnectionMultiplexer> Factory(ConfigurationOptions configuration, TextWriter log)
        {
            factoryCalls++;
            return Task.FromResult(CreateMultiplexer(isConnected: true));
        }

        var pool = CreatePool(Factory);

        pool.Dispose();

        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    public async Task ConnectAsync_overlapping_requests_share_one_factory_run()
    {
        // GetOrCreateTask serializes racing callers on the slot lock: per reset the
        // factory runs at most once and every caller awaits the SAME in-flight task.
        // The factory is gated on a TaskCompletionSource, so each pool.ConnectAsync
        // call below runs synchronously exactly up to the await on the slot's pending
        // task - all eight requests verifiably overlap without any timing or sleeps.
        var connected = CreateMultiplexer(isConnected: true);
        var gate = new TaskCompletionSource<IConnectionMultiplexer>();
        var factoryCalls = 0;

        Task<IConnectionMultiplexer> Factory(ConfigurationOptions configuration, TextWriter log)
        {
            factoryCalls++;
            return gate.Task;
        }

        using var pool = CreatePool(Factory);

        var requests = Enumerable.Range(0, 8).Select(_ => pool.ConnectAsync()).ToArray();

        Assert.Equal(1, factoryCalls); // one connect cycle despite eight waiters
        Assert.All(requests, request => Assert.False(request.IsCompleted));

        gate.SetResult(connected);
        var results = await Task.WhenAll(requests);

        Assert.Equal(1, factoryCalls);
        Assert.All(results, result => Assert.Same(connected, result));
    }

    [Fact]
    public async Task ConnectAsync_keeps_retrying_when_failed_attempt_dispose_throws()
    {
        // The cleanup between attempts disposes the failed multiplexer inside a
        // try/catch. If that guard were removed, the throwing Dispose below would abort
        // the retry loop on the very first attempt and the slot could never recover.
        // Every failed attempt here yields a disconnected multiplexer whose Dispose
        // throws, and Redis "comes back" on the final attempt: the run must survive all
        // the throwing Disposes and still hand out the live connection.
        var connected = CreateMultiplexer(isConnected: true);
        var disconnected = new List<IConnectionMultiplexer>();
        var factoryCalls = 0;

        Task<IConnectionMultiplexer> Factory(ConfigurationOptions configuration, TextWriter log)
        {
            factoryCalls++;
            if (factoryCalls < AsyncLazyRedisConnection.MaxConnectAttempts)
            {
                var multiplexer = CreateMultiplexer(isConnected: false);
                multiplexer.When(m => m.Dispose()).Do(_ => throw new InvalidOperationException("dispose failed"));
                disconnected.Add(multiplexer);
                return Task.FromResult(multiplexer);
            }

            return Task.FromResult(connected);
        }

        using var pool = CreatePool(Factory);

        var result = await pool.ConnectAsync();

        Assert.Same(connected, result);
        Assert.Equal(AsyncLazyRedisConnection.MaxConnectAttempts, factoryCalls);

        // Each failed-attempt multiplexer had Dispose attempted exactly once; the throw
        // was swallowed instead of aborting the remaining attempts.
        Assert.Equal(AsyncLazyRedisConnection.MaxConnectAttempts - 1, disconnected.Count);
        foreach (var multiplexer in disconnected)
            multiplexer.Received(1).Dispose();
    }

    /// <summary>
    ///     Pool with the injected factory, the given slot count (one by default) and a
    ///     zero retry delay so the MaxConnectAttempts retry loop completes without real
    ///     waiting. Production keeps the default 2-second delay via the public
    ///     constructors.
    /// </summary>
    private static RedisConnectionPool CreatePool(
        Func<ConfigurationOptions, TextWriter, Task<IConnectionMultiplexer>> connectionFactory,
        uint poolSize = 1)
    {
        return new RedisConnectionPool(Options.Create(CreateRedisOptions(poolSize)), NullLoggerFactory.Instance,
            connectionFactory, TimeSpan.Zero);
    }

    /// <summary>
    ///     Standalone slot with the injected factory and a zero retry delay, for tests
    ///     that exercise the slot's own accessors rather than the pool's selection.
    /// </summary>
    private static AsyncLazyRedisConnection CreateSlot(
        Func<ConfigurationOptions, TextWriter, Task<IConnectionMultiplexer>> connectionFactory)
    {
        return new AsyncLazyRedisConnection(CreateRedisOptions(poolSize: 1),
            NullLogger<AsyncLazyRedisConnection>.Instance, connectionFactory, TimeSpan.Zero);
    }

    private static CapRedisOptions CreateRedisOptions(uint poolSize)
    {
        return new CapRedisOptions
        {
            Configuration = ConfigurationOptions.Parse("localhost:6379"),
            ConnectionPoolSize = poolSize
        };
    }

    /// <summary>
    ///     Fake multiplexer with a fixed <see cref="IConnectionMultiplexer.IsConnected" />
    ///     state. Event subscription (used by <c>LogEvents</c>) works out of the box on
    ///     the substitute; <see cref="IConnectionMultiplexer.GetCounters()" /> returns
    ///     empty counters so the pool's least-loaded selection sees zero outstanding
    ///     operations. Dispose calls are counted by NSubstitute.
    /// </summary>
    private static IConnectionMultiplexer CreateMultiplexer(bool isConnected)
    {
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.IsConnected.Returns(isConnected);
        multiplexer.GetCounters().Returns(new ServerCounters(new DnsEndPoint("localhost", 6379)));
        return multiplexer;
    }
}

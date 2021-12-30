// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams
{
    internal class RedisConnectionPool : IRedisConnectionPool, IDisposable
    {
        private readonly ConcurrentBag<AsyncLazyRedisConnection> _connections = new();

        private readonly ILoggerFactory _loggerFactory;
        private readonly SemaphoreSlim _poolLock = new(1);
        private readonly CapRedisOptions _redisOptions;
        private bool _isDisposed;
        private bool _poolAlreadyConfigured;

        public RedisConnectionPool(IOptions<CapRedisOptions> options, ILoggerFactory loggerFactory)
        {
            _redisOptions = options.Value;
            _loggerFactory = loggerFactory;
            Init().GetAwaiter().GetResult();
        }

        private AsyncLazyRedisConnection? QuietConnection
        {
            get
            {
                return _poolAlreadyConfigured ? _connections.OrderBy(async c => (await c).ConnectionCapacity).First() : null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<IConnectionMultiplexer> ConnectAsync()
        {
            if (QuietConnection == null)
            {
                _poolAlreadyConfigured = _connections.Count(c => c.IsValueCreated) == _redisOptions.ConnectionPoolSize;
                if (QuietConnection != null)
                    return (await QuietConnection).Connection;
            }

            foreach (var lazy in _connections)
            {
                if (!lazy.IsValueCreated)
                    return (await lazy).Connection;

                var connection = await lazy;
                if (connection.ConnectionCapacity == default)
                    return connection.Connection;
            }

            return (await _connections.OrderBy(async c => (await c).ConnectionCapacity).First()).Connection;
        }

        private async Task Init()
        {
            try
            {
                await _poolLock.WaitAsync();

                if (_connections.Any())
                    return;

                for (var i = 0; i < _redisOptions.ConnectionPoolSize; i++)
                {
                    var connection = new AsyncLazyRedisConnection(_redisOptions,
                        _loggerFactory.CreateLogger<AsyncLazyRedisConnection>());

                    _connections.Add(connection);
                }
            }
            finally
            {
                _poolLock.Release();
            }
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
                foreach (var connection in _connections)
                {
                    if (!connection.IsValueCreated)
                        continue;

                    connection.GetAwaiter().GetResult().Dispose();
                }

            _isDisposed = true;
        }
    }
}
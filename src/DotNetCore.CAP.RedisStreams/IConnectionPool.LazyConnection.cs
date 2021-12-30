// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams
{
    public class AsyncLazyRedisConnection : Lazy<Task<RedisConnection>>
    {
        public AsyncLazyRedisConnection(CapRedisOptions redisOptions,
            ILogger<AsyncLazyRedisConnection> logger) : base(() => ConnectAsync(redisOptions, logger))
        {
        }

        public TaskAwaiter<RedisConnection> GetAwaiter()
        {
            return Value.GetAwaiter();
        }

        private static async Task<RedisConnection> ConnectAsync(CapRedisOptions redisOptions,
            ILogger<AsyncLazyRedisConnection> logger)
        {
            int attemp = 1;

            var redisLogger = new RedisLogger(logger);

            ConnectionMultiplexer? connection = null;

            while (attemp <= 5)
            {
                connection = await ConnectionMultiplexer.ConnectAsync(redisOptions.Configuration, redisLogger)
                .ConfigureAwait(false);

                connection.LogEvents(logger);

                if (!connection.IsConnected)
                {
                    logger.LogWarning($"Can't establish redis connection,trying to establish connection [attemp {attemp}].");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    ++attemp;
                }
                else
                    attemp = 6;
            }
            if (connection == null)
                throw new Exception($"Can't establish redis connection,after [{attemp}] attemps.");

            return new RedisConnection(connection);
        }
    }

    public class RedisConnection : IDisposable
    {
        private bool _isDisposed;

        public RedisConnection(IConnectionMultiplexer connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public IConnectionMultiplexer Connection { get; }
        public long ConnectionCapacity => Connection.GetCounters().TotalOutstanding;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing) Connection.Dispose();

            _isDisposed = true;
        }
    }
}
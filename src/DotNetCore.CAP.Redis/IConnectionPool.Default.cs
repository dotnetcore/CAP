using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    class RedisConnectionPool : IRedisConnectionPool,IDisposable
    {
        private readonly ConcurrentBag<AsyncLazyRedisConnection> connections = new ConcurrentBag<AsyncLazyRedisConnection>();
        private readonly SemaphoreSlim poolLock = new SemaphoreSlim(1);
        private readonly CapRedisOptions redisOptions;
        private readonly ILoggerFactory loggerFactory;
        private bool poolAlreadyConfigured = false;
        private bool isDisposed;

        private AsyncLazyRedisConnection QuietConnection
        {
            get
            {
                if (poolAlreadyConfigured)
                    return connections.OrderBy(async c => (await c).ConnectionCapacity).First();
                else
                    return null;
            }
        }

        public RedisConnectionPool(IOptions<CapRedisOptions> options, ILoggerFactory loggerFactory)
        {
            redisOptions = options.Value;
            this.loggerFactory = loggerFactory;
            Init().GetAwaiter().GetResult();
        }

        public async Task<IConnectionMultiplexer> ConnectAsync()
        {
            if (QuietConnection == null)
            {
                poolAlreadyConfigured = connections.Count(c => c.IsValueCreated) == redisOptions.ConnectionPoolSize;
                if (QuietConnection != null)
                    return (await QuietConnection).Connection;
            }

            foreach (var lazy in connections)
            {
                if (!lazy.IsValueCreated)
                    return (await lazy).Connection;

                var connection = await lazy;
                if (connection.ConnectionCapacity == default)
                    return connection.Connection;
            }

            return (await connections.OrderBy(async c => (await c).ConnectionCapacity).First()).Connection;
        }

        private async Task Init()
        {
            try
            {
                await poolLock.WaitAsync();

                if (connections.Any())
                    return;

                for (int i = 0; i < redisOptions.ConnectionPoolSize; i++)
                {
                    var connection = new AsyncLazyRedisConnection(redisOptions, loggerFactory.CreateLogger<AsyncLazyRedisConnection>());

                    connections.Add(connection);
                }
            }
            finally
            {
                poolLock.Release();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
            {
                foreach (var connection in this.connections)
                {
                    if (!connection.IsValueCreated)
                        continue;

                    connection.GetAwaiter().GetResult().Dispose();
                }
            }

            isDisposed = true;
        }
    }
}

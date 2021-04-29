using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.RedisStreams
{
    public class AsyncLazyRedisConnection : Lazy<Task<RedisConnection>>
    {
        public AsyncLazyRedisConnection(CapRedisOptions redisOptions, ILogger<AsyncLazyRedisConnection> logger)
            : base(() => ConnectAsync(redisOptions, logger))
        {
        }

        public TaskAwaiter<RedisConnection> GetAwaiter() { return Value.GetAwaiter(); }

        static async Task<RedisConnection> ConnectAsync(CapRedisOptions redisOptions, ILogger<AsyncLazyRedisConnection> logger)
        {
            var redisLogger = new RedisLogger(logger);

            var connection = await ConnectionMultiplexer.ConnectAsync(redisOptions.Configuration, redisLogger).ConfigureAwait(false);

            connection.LogEvents(logger);

            return new RedisConnection(connection);
        }
    }

    public class RedisConnection:IDisposable
    {
        private bool isDisposed;

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

        void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
            {
                Connection.Dispose();
            }

            isDisposed = true;
        }
    }
}

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    class RedisCacheManager : IRedisCacheManager
    {
        private readonly SemaphoreSlim connectionLock = new SemaphoreSlim(1, 1);
        private readonly CapRedisOptions options;
        private readonly ILogger<RedisCacheManager> logger;
        private ConnectionMultiplexer redis;
        private ISubscriber subscriber;
        private bool disposed;

        public RedisCacheManager(IOptions<CapRedisOptions> options, ILogger<RedisCacheManager> logger)
        {
            this.options = options.Value;
            this.logger = logger;
            _ = ConnectAsync();
        }

        public async Task SubscribeAsync(string channelName, IEnumerable<string> topics, Func<RedisChannel, RedisMessage, Task> callback)
        {
            var channel = await GetChannel(channelName);
            channel.OnMessage(channelMessage =>
            {
                var message = RedisMessage.Create(channelMessage.Message);

                if (topics.Any(c => c == message.GetName()))
                {
                    callback?.Invoke(channelMessage.SubscriptionChannel, message);
                }
            });
        }

        public async Task PublishAsync(string channelName, RedisValue message)
        {
            await subscriber.PublishAsync(channelName, message);
        }
        public async Task UnsubscribeAsync()
        {
            await subscriber?.UnsubscribeAllAsync();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                subscriber?.UnsubscribeAll();
                redis?.Close();
                disposed = true;
            }
        }


        private async Task<ChannelMessageQueue> GetChannel(string channelName)
        {
            await ConnectAsync().ConfigureAwait(false);
            return await subscriber.SubscribeAsync(channelName).ConfigureAwait(false);
        }

        private async Task ConnectAsync()
        {
            if (disposed == true)
                throw new ObjectDisposedException(nameof(IRedisCacheManager));

            if (redis != null)
            {
                subscriber ??= redis.GetSubscriber();
                return;
            }
            else
            {
                try
                {
                    await connectionLock.WaitAsync().ConfigureAwait(false);

                    if (redis != null)
                    {
                        return;
                    }

                    var redisLogger = new RedisCacheLogger(logger);

                    redis = await ConnectionMultiplexer.ConnectAsync(options.Configuration, redisLogger)
                        .ConfigureAwait(false);

                    redis.LogEvents(logger);

                    subscriber = redis.GetSubscriber();
                }
                finally
                {
                    connectionLock.Release();
                }
            }
        }

    }
}

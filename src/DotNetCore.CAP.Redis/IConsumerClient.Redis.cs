using DotNetCore.CAP.Redis;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    class RedisConsumerClient : IConsumerClient
    {
        private readonly ILogger<RedisConsumerClient> logger;
        private readonly IRedisCacheManager redis;
        private readonly CapRedisOptions options;
        private readonly string groupId;

        public RedisConsumerClient(
            string groubId,
            IRedisCacheManager redis,
            CapRedisOptions options,
            ILogger<RedisConsumerClient> logger
            )
        {
            this.groupId = groubId;
            this.redis = redis;
            this.options = options;
            this.logger = logger;
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("redis", options.Endpoint);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null) throw new ArgumentNullException(nameof(topics));

            redis.SubscribeAsync(groupId, topics, (channel, message) =>
            {
                logger.LogDebug($"Redis message with name {message.GetName()} subscribed.");

                message.GroupId = groupId;

                OnMessageReceived?.Invoke(channel, message);

                return Task.CompletedTask;
            });
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            cancellationToken.Register(async () => await redis.UnsubscribeAsync());

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
        }

        public void Commit(object sender)
        {
            // ignore
        }

        public void Reject(object sender)
        {
            // ignore
        }

        public void Dispose()
        {
            //ignore
        }

    }
}

using DotNetCore.CAP.Redis;
using DotNetCore.CAP;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    class RedisConsumerClientFactory : IConsumerClientFactory
    {
        private readonly CapRedisOptions redisOptions;
        private readonly IRedisCacheManager redis;
        private readonly ILogger<RedisConsumerClient> logger;

        public RedisConsumerClientFactory(IOptions<CapRedisOptions> redisOptions, IRedisCacheManager redis, ILogger<RedisConsumerClient> logger)
        {
            this.redisOptions = redisOptions.Value;
            this.redis = redis;
            this.logger = logger;
        }

        public IConsumerClient Create(string groupId)
        {
            return new RedisConsumerClient(groupId, redis, redisOptions, logger);
        }
    }
}

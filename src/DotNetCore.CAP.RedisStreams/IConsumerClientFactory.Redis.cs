// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.RedisStreams
{
    internal class RedisConsumerClientFactory : IConsumerClientFactory
    {
        private readonly ILogger<RedisConsumerClient> logger;
        private readonly IRedisStreamManager redis;
        private readonly CapRedisOptions redisOptions;

        public RedisConsumerClientFactory(IOptions<CapRedisOptions> redisOptions, IRedisStreamManager redis,
            ILogger<RedisConsumerClient> logger)
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
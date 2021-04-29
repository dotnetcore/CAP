// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.RedisStreams
{
    internal class RedisConsumerClientFactory : IConsumerClientFactory
    {
        private readonly ILogger<RedisConsumerClient> _logger;
        private readonly IRedisStreamManager _redis;
        private readonly IOptions<CapRedisOptions> _redisOptions;

        public RedisConsumerClientFactory(IOptions<CapRedisOptions> redisOptions, IRedisStreamManager redis,
            ILogger<RedisConsumerClient> logger)
        {
            _redisOptions = redisOptions;
            _redis = redis;
            _logger = logger;
        }

        public IConsumerClient Create(string groupId)
        {
            return new RedisConsumerClient(groupId, _redis, _redisOptions, _logger);
        }
    }
}
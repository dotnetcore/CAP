// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.RedisStreams;

internal class RedisConsumerClientFactory(IOptions<CapRedisOptions> _redisOptions, IRedisStreamManager _redis, ILogger<RedisConsumerClient> _logger) : IConsumerClientFactory
{
    public Task<IConsumerClient> CreateAsync(string groupName, byte groupConcurrent)
    {
        var client = new RedisConsumerClient(groupName, groupConcurrent, _redis, _redisOptions, _logger);
        return Task.FromResult<IConsumerClient>(client);
    }
}
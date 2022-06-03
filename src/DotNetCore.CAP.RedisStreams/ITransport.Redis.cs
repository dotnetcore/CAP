// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.RedisStreams
{
    internal class RedisTransport : ITransport
    {
        private readonly ILogger<RedisTransport> _logger;
        private readonly CapRedisOptions _options;
        private readonly IRedisStreamManager _redis;

        public RedisTransport(IRedisStreamManager redis, IOptions<CapRedisOptions> options,
            ILogger<RedisTransport> logger)
        {
            _redis = redis;
            _options = options.Value;
            _logger = logger;
        }

        public BrokerAddress BrokerAddress => new ("redis", _options.Endpoint);

        public async Task<OperateResult> SendAsync(TransportMessage message)
        {
            try
            {
                await _redis.PublishAsync(message.GetName(), message.AsStreamEntries())
                    .ConfigureAwait(false);

                _logger.LogDebug($"Redis message [{message.GetName()}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wrapperEx);
            }
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.RedisStreams;

internal class RedisTransport(
    IRedisStreamManager _redis,
    IOptions<CapRedisOptions> options,
    ILogger<RedisTransport> _logger) : ITransport
{
    private readonly CapRedisOptions _options = options.Value;

    public BrokerAddress BrokerAddress => new("redis", _options.Endpoint);

    public async Task<OperateResult> SendAsync(TransportMessage message)
    {
        try
        {
            await _redis.PublishAsync(message.GetName(), message.AsStreamEntries())
                .ConfigureAwait(false);

            _logger.LogDebug("Redis message [{message}] has been published.",message.GetName());

            return OperateResult.Success;
        }
        catch (Exception ex)
        {
            var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

            return OperateResult.Failed(wrapperEx);
        }
    }
}
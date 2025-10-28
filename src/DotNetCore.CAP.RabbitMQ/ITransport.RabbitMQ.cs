// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace DotNetCore.CAP.RabbitMQ;

internal sealed class RabbitMqTransport : ITransport
{
    private readonly IConnectionChannelPool _connectionChannelPool;
    private readonly string _exchange;
    private readonly ILogger _logger;

    public RabbitMqTransport(ILogger<RabbitMqTransport> logger, IConnectionChannelPool connectionChannelPool)
    {
        _logger = logger;
        _connectionChannelPool = connectionChannelPool;
        _exchange = _connectionChannelPool.Exchange;
    }

    public BrokerAddress BrokerAddress => new("RabbitMQ", _connectionChannelPool.HostAddress);

    public async Task<OperateResult> SendAsync(TransportMessage message)
    {
        IChannel? channel = null;
        try
        {
            channel = await _connectionChannelPool.Rent();

            var props = new BasicProperties
            {
                MessageId = message.GetId(),
                DeliveryMode = DeliveryModes.Persistent,
                Headers = message.Headers.ToDictionary(x => x.Key, object? (x) => x.Value)
            };

            await channel.BasicPublishAsync(_exchange, message.GetName(), false, props, message.Body);

            _logger.LogInformation("CAP message '{0}' published, internal id '{1}'", message.GetName(), message.GetId());

            return OperateResult.Success;
        }
        catch (Exception ex)
        {
            if (ex is AlreadyClosedException && channel?.IsOpen == true)
            {
                // There are cases when channel's property IsOpen returns true, but the connection is actually closed, e.g. https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/1871
                // This is a workaround to abort the channel in this case to avoid returning a faulty channel back to the pool.

                _logger.LogWarning("Channel state inconsistency detected: channel is reported as open, but its underlying connection is closed. Forcing channel closure.");

                await channel.DisposeAsync();
            }

            var wrapperEx = new PublisherSentFailedException(ex.Message, ex);
            var errors = new OperateError
            {
                Code = ex.HResult.ToString(),
                Description = ex.Message
            };

            return OperateResult.Failed(wrapperEx, errors);
        }
        finally
        {
            if (channel != null) _connectionChannelPool.Return(channel);
        }
    }
}
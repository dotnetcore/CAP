// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQTransport : ITransport
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly ILogger _logger;
        private readonly string _exchangeName;
        private readonly string _exchangeType;
        private readonly RabbitMQOptions _rabbitMQOptions;

        public RabbitMQTransport(
            ILogger<RabbitMQTransport> logger,
            IConnectionChannelPool connectionChannelPool,
            IOptions<RabbitMQOptions> options)
        {
            _logger = logger;
            _connectionChannelPool = connectionChannelPool;
            _exchangeName = _connectionChannelPool.Exchange;
            _rabbitMQOptions = options.Value;
            _exchangeType = _rabbitMQOptions.ExChangeType;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("RabbitMQ", _connectionChannelPool.HostAddress);

        public Task<OperateResult> SendAsync(TransportMessage message)
        {
            IModel channel = null;
            try
            {
                channel = _connectionChannelPool.Rent();
                var props = new BasicProperties
                {
                    DeliveryMode = 2,
                    Headers = message.Headers.ToDictionary(x => x.Key, x => (object)x.Value)
                };

                channel.ExchangeDeclare(_exchangeName, _exchangeType, true);

                channel.BasicPublish(_exchangeName, message.GetName(), props, message.Body);

                _logger.LogDebug($"RabbitMQ topic message [{message.GetName()}] has been published.");

                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);
                var errors = new OperateError
                {
                    Code = ex.HResult.ToString(),
                    Description = ex.Message
                };

                return Task.FromResult(OperateResult.Failed(wrapperEx, errors));
            }
            finally
            {
                if (channel != null)
                {
                    var returned = _connectionChannelPool.Return(channel);
                    if (!returned)
                    {
                        channel.Dispose();
                    }
                }
            }
        }
    }
}
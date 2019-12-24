﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQTransport : ITransport
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly ILogger _logger;
        private readonly string _exchange;

        public RabbitMQTransport(
            ILogger<RabbitMQTransport> logger,
            IConnectionChannelPool connectionChannelPool)
        {
            _logger = logger;
            _connectionChannelPool = connectionChannelPool;
            _exchange = _connectionChannelPool.Exchange;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("RabbitMQ", _connectionChannelPool.HostAddress);

        public Task<OperateResult> SendAsync(TransportMessage message)
        {
            var channel = _connectionChannelPool.Rent();
            try
            {
                var props = new BasicProperties
                {
                    DeliveryMode = 2,
                    Headers = message.Headers.ToDictionary(x => x.Key, x => (object)x.Value)
                };

                channel.ExchangeDeclare(_exchange, RabbitMQOptions.ExchangeType, true);

                channel.BasicPublish(_exchange, message.GetName(), props, message.Body);

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
                var returned = _connectionChannelPool.Return(channel);
                if (!returned)
                {
                    channel.Dispose();
                }
            }
        } 
    }
}
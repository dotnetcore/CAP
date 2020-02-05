// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;

        public RabbitMQConsumerClientFactory(IOptions<RabbitMQOptions> rabbitMQOptions, IConnectionChannelPool channelPool)
        {
            _rabbitMQOptions = rabbitMQOptions;
            _connectionChannelPool = channelPool;
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
               var client = new RabbitMQConsumerClient(groupId, _connectionChannelPool, _rabbitMQOptions);
               client.Connect();
               return client;
            }
            catch (System.Exception e)
            {
                throw new BrokerConnectionException(e);
            }
        }
    }
}
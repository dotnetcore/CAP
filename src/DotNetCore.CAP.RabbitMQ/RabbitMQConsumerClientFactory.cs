﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly RabbitMQOptions _rabbitMQOptions;


        public RabbitMQConsumerClientFactory(RabbitMQOptions rabbitMQOptions, IConnectionChannelPool channelPool)
        {
            _rabbitMQOptions = rabbitMQOptions;
            _connectionChannelPool = channelPool;
        }

        public IConsumerClient Create(string groupId)
        {
            return new RabbitMQConsumerClient(groupId, _connectionChannelPool, _rabbitMQOptions);
        }
    }
}
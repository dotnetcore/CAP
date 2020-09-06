// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.ZeroMQ
{
    internal sealed class ZeroMQConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly IOptions<ZeroMQOptions> _ZeroMQOptions;

        public ZeroMQConsumerClientFactory(IOptions<ZeroMQOptions> ZeroMQOptions, IConnectionChannelPool channelPool)
        {
            _ZeroMQOptions = ZeroMQOptions;
            _connectionChannelPool = channelPool;
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
                var client = new ZeroMQConsumerClient(groupId, _connectionChannelPool, _ZeroMQOptions);
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
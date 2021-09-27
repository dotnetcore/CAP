// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.NATS
{
    internal sealed class NATSConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IOptions<NATSOptions> _natsOptions;

        public NATSConsumerClientFactory(IOptions<NATSOptions> natsOptions)
        {
            _natsOptions = natsOptions;
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
                var client = new NATSConsumerClient(groupId, _natsOptions);
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
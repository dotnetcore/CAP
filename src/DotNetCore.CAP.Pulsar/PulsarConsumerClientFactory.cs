// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    internal sealed class PulsarConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IOptions<PulsarOptions> _pulsarOptions;

        public PulsarConsumerClientFactory(IOptions<PulsarOptions> pulsarOptions)
        {
            _pulsarOptions = pulsarOptions;
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
                var client = new PulsarConsumerClient(groupId, _pulsarOptions);
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
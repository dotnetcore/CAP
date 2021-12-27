// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulsar.Client.Api;

namespace DotNetCore.CAP.Pulsar
{
    internal sealed class PulsarConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IConnectionFactory _connection;
        private readonly IOptions<PulsarOptions> _pulsarOptions;

        public PulsarConsumerClientFactory(IConnectionFactory connection, ILoggerFactory loggerFactory, IOptions<PulsarOptions> pulsarOptions)
        {
            _connection = connection;
            _pulsarOptions = pulsarOptions;

            if (_pulsarOptions.Value.EnableClientLog)
            {
                PulsarClient.Logger = loggerFactory.CreateLogger<PulsarClient>();
            }
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
                var client = _connection.RentClient();
                var consumerClient = new PulsarConsumerClient(client, groupId, _pulsarOptions);
                return consumerClient;
            }
            catch (System.Exception e)
            {
                throw new BrokerConnectionException(e);
            }
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    internal sealed class KafkaConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IOptions<KafkaOptions> _kafkaOptions;

        public KafkaConsumerClientFactory(IOptions<KafkaOptions> kafkaOptions)
        {
            _kafkaOptions = kafkaOptions;
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
                var client = new KafkaConsumerClient(groupId, _kafkaOptions);
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
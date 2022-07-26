// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.GooglePubSub
{
    internal sealed class GcpPubSubMongoConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IOptions<GcpPubSubMongoOptions> _options;
        private GcpPubSubMongoConsumerClient consumerClient;

        public GcpPubSubMongoConsumerClientFactory(IOptions<GcpPubSubMongoOptions> options)
        {
            _options = options;
        }
        
        public IConsumerClient Create(string groupId)
        {

            try
            {
                if (consumerClient == null)
                {
                    consumerClient = new GcpPubSubMongoConsumerClient(groupId, _options);
                    consumerClient.Connect();
                }
                return consumerClient;
            }
            catch (System.Exception e)
            {
                throw new BrokerConnectionException(e);
            }
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.GooglePubSub
{
    internal sealed class GooglePubSubConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IOptions<GooglePubSubOptions> _options;
        private GooglePubSubConsumerClient consumerClient;

        public GooglePubSubConsumerClientFactory(IOptions<GooglePubSubOptions> options)
        {
            _options = options;
        }
        
        public IConsumerClient Create(string groupId)
        {

            try
            {
                if (consumerClient == null)
                {
                    consumerClient = new GooglePubSubConsumerClient(groupId, _options);
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
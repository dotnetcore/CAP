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
        private bool _isOnlyOneGroup = true;

        public GooglePubSubConsumerClientFactory(IOptions<GooglePubSubOptions> options)
        {
            _options = options;
        }

        public IConsumerClient Create(string groupId)
        {
            if (_isOnlyOneGroup)
            {
                _isOnlyOneGroup = false;
            }
            else
            {
                throw new NotSupportedException("Google Pub/Sub does not support 'Subscriber Group' !!!");
            }
            try
            {
                var client = new GooglePubSubConsumerClient(groupId, _options);
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
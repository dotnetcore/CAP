// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.GooglePubSub
{
    internal sealed class GooglePubSubConsumerClientFactory : IConsumerClientFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptions<GooglePubSubOptions> _options;

        public GooglePubSubConsumerClientFactory(ILoggerFactory loggerFactory, IOptions<GooglePubSubOptions> options)
        {
            _loggerFactory = loggerFactory;
            _options = options;
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
                var logger = _loggerFactory.CreateLogger(typeof(GooglePubSubConsumerClient));
                var client = new GooglePubSubConsumerClient(logger, groupId, _options);
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
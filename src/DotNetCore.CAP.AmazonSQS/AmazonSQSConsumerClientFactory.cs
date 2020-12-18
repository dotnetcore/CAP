// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AmazonSQS
{
    internal sealed class AmazonSQSConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IOptions<AmazonSQSOptions> _amazonSQSOptions;

        public AmazonSQSConsumerClientFactory(IOptions<AmazonSQSOptions> amazonSQSOptions)
        {
            _amazonSQSOptions = amazonSQSOptions;
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
               var client = new AmazonSQSConsumerClient(groupId, _amazonSQSOptions);
               return client;
            }
            catch (System.Exception e)
            {
                throw new BrokerConnectionException(e);
            }
        }
    }
}
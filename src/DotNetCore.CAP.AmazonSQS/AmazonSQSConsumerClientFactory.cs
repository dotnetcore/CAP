// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AmazonSQS;

internal sealed class AmazonSQSConsumerClientFactory : IConsumerClientFactory
{
    private readonly IOptions<AmazonSQSOptions> _amazonSQSOptions;

    public AmazonSQSConsumerClientFactory(IOptions<AmazonSQSOptions> amazonSQSOptions)
    {
        _amazonSQSOptions = amazonSQSOptions;
    }

    public Task<IConsumerClient> CreateAsync(string groupName, byte groupConcurrent)
    {
        try
        {
            var client = new AmazonSQSConsumerClient(groupName, groupConcurrent, _amazonSQSOptions);
            return Task.FromResult<IConsumerClient>(client);
        }
        catch (Exception e)
        {
            throw new BrokerConnectionException(e);
        }
    }
}
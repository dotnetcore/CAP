// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka;

public class KafkaConsumerClientFactory : IConsumerClientFactory
{
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly IServiceProvider _serviceProvider;

    public KafkaConsumerClientFactory(IOptions<KafkaOptions> kafkaOptions, IServiceProvider serviceProvider)
    {
        _kafkaOptions = kafkaOptions;
        _serviceProvider = serviceProvider;
    }

    public virtual IConsumerClient Create(string groupName, byte groupConcurrent)
    {
        try
        {
            return new KafkaConsumerClient(groupName, groupConcurrent, _kafkaOptions, _serviceProvider);
        }
        catch (Exception e)
        {
            throw new BrokerConnectionException(e);
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.NATS;

internal sealed class NATSConsumerClientFactory : IConsumerClientFactory
{
    private readonly IOptions<NATSOptions> _natsOptions;
    private readonly IServiceProvider _serviceProvider;

    public NATSConsumerClientFactory(IOptions<NATSOptions> natsOptions, IServiceProvider serviceProvider)
    {
        _natsOptions = natsOptions;
        _serviceProvider = serviceProvider;
    }

    public IConsumerClient Create(string groupName, byte groupConcurrent)
    {
        try
        {
            var client = new NATSConsumerClient(groupName, groupConcurrent, _natsOptions, _serviceProvider);
            client.Connect();
            return client;
        }
        catch (System.Exception e)
        {
            throw new BrokerConnectionException(e);
        }
    }
}
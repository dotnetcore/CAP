// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AzureServiceBus;

internal sealed class AzureServiceBusConsumerClientFactory : IConsumerClientFactory
{
    private readonly IOptions<AzureServiceBusOptions> _asbOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;

    public AzureServiceBusConsumerClientFactory(
        ILoggerFactory loggerFactory,
        IOptions<AzureServiceBusOptions> asbOptions,
        IServiceProvider serviceProvider)
    {
        _loggerFactory = loggerFactory;
        _asbOptions = asbOptions;
        _serviceProvider = serviceProvider;
    }

    public IConsumerClient Create(string groupName, byte groupConcurrent)
    {
        try
        {
            var logger = _loggerFactory.CreateLogger(typeof(AzureServiceBusConsumerClient));
            var client = new AzureServiceBusConsumerClient(logger, groupName, groupConcurrent, _asbOptions, _serviceProvider);
            client.ConnectAsync().GetAwaiter().GetResult();
            return client;
        }
        catch (Exception e)
        {
            throw new BrokerConnectionException(e);
        }
    }
}
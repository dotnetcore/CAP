// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulsar.Client.Api;

namespace DotNetCore.CAP.Pulsar;

internal sealed class PulsarConsumerClientFactory : IConsumerClientFactory
{
    private readonly IConnectionFactory _connection;
    private readonly IOptions<PulsarOptions> _pulsarOptions;

    public PulsarConsumerClientFactory(IConnectionFactory connection, ILoggerFactory loggerFactory,
        IOptions<PulsarOptions> pulsarOptions)
    {
        _connection = connection;
        _pulsarOptions = pulsarOptions;

        if (_pulsarOptions.Value.EnableClientLog) PulsarClient.Logger = loggerFactory.CreateLogger<PulsarClient>();
    }

    public IConsumerClient Create(string groupName, byte groupConcurrent)
    {
        try
        {
            var client = _connection.RentClient();
            var consumerClient = new PulsarConsumerClient(_pulsarOptions, client, groupName, groupConcurrent);
            return consumerClient;
        }
        catch (Exception e)
        {
            throw new BrokerConnectionException(e);
        }
    }
}
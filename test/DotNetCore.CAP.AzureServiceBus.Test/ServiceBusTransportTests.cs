// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sample.AzureServiceBus.InMemory.Contracts.DomainEvents;
using Shouldly;
using Xunit;

namespace DotNetCore.CAP.AzureServiceBus.Tests;

public class ServiceBusTransportTests
{
    private readonly IOptions<AzureServiceBusOptions> _options;

    public ServiceBusTransportTests()
    {
        var config = new AzureServiceBusOptions();
        config.ConfigureCustomProducer<EntityCreated>(cfg => cfg.WithTopic("entity-created"));

        _options = Options.Create(config);
    }

    [Fact]
    public void Custom_Producer_Should_Have_Custom_Topic()
    {
        // Given
        var transport = new AzureServiceBusTransport(NullLogger<AzureServiceBusTransport>.Instance, _options);

        var transportMessage = new TransportMessage(
            headers: new Dictionary<string, string?>()
            {
                {Headers.MessageName, nameof(EntityCreated)}
            },
            body: null);

        // When
        var producer = transport.CreateProducerForMessage(transportMessage);

        // Then
        producer.MessageTypeName.ShouldBe(nameof(EntityCreated));
        producer.TopicPath.ShouldBe("entity-created");
    }

    [Fact]
    public void Default_Producer_Should_Have_Default_Topic()
    {
        // Given
        var transport = new AzureServiceBusTransport(NullLogger<AzureServiceBusTransport>.Instance, _options);

        var transportMessage = new TransportMessage(
            headers: new Dictionary<string, string?>()
            {
                {Headers.MessageName, nameof(EntityDeleted)}
            },
            body: null);

        // When
        var producer = transport.CreateProducerForMessage(transportMessage);

        // Then
        producer.MessageTypeName.ShouldBe(nameof(EntityDeleted));
        producer.TopicPath.ShouldBe(_options.Value.TopicPath);
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using DotNetCore.CAP.AzureServiceBus;
using DotNetCore.CAP.AzureServiceBus.Consumer;
using DotNetCore.CAP.AzureServiceBus.Producer;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP;

/// <summary>
/// Provides programmatic configuration for the CAP Azure Service Bus project.
/// </summary>
public class AzureServiceBusOptions : AzureServiceBusOptionsBase
{
    /// <summary>
    /// TopicPath default value for CAP.
    /// </summary>
    public const string DefaultTopicPath = "cap";

    /// <summary>
    /// Azure Service Bus Namespace connection string. Must not contain topic information.
    /// </summary>
    public string ConnectionString { get; set; } = default!;

    /// <summary>
    /// Namespace of service bus , Needs to be set when using with TokenCredential Property
    /// </summary>
    public string Namespace { get; set; } = default!;
    
    /// <summary>
    /// The name of the topic relative to the service namespace base address.
    /// </summary>
    public string TopicPath { get; set; } = DefaultTopicPath;

    /// <summary>
    /// Represents the Azure Active Directory token provider for Azure Managed Service Identity integration.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }
    
    internal ICollection<IServiceBusProducerDescriptor> CustomProducers { get; set; } =
        new List<IServiceBusProducerDescriptor>();

    public AzureServiceBusOptions ConfigureCustomProducer<T>(
        Action<ServiceBusProducerDescriptorBuilder<T>> configuration)
    {
        var builder = new ServiceBusProducerDescriptorBuilder<T>();
        configuration(builder);
        CustomProducers.Add(builder.Build());

        return this;
    } 
    
    internal IDictionary<string, IServiceBusConsumerDescriptor> CustomConsumers{ get; set; } =
        new Dictionary<string, IServiceBusConsumerDescriptor>();

    public AzureServiceBusOptions ConfigureCustomTopicConsumer(string groupName,
        Action<ServiceBusConsumerDescriptorBuilder> configuration)
    {
        var builder = new ServiceBusConsumerDescriptorBuilder(groupName);
        configuration(builder);
        CustomConsumers.Add(builder.Build(this));

        return this;
    }
}

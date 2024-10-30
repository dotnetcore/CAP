// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Azure.Core;

namespace DotNetCore.CAP.AzureServiceBus.Consumer;

public class ServiceBusConsumerDescriptorBuilder
{
    /// <summary>
    /// Name of the group that the consumer is part of.
    /// </summary>
    private string GroupName { get; set; } = null!;
    
    private string TopicPath { get; set; } = null!;
    
    private AzureServiceBusOptions Options { get; set; } = new ();
    
    private bool DefaultOptions { get; set; } = false;
    
    public ServiceBusConsumerDescriptorBuilder(string groupName)
    {
        GroupName = groupName;
    }

    public ServiceBusConsumerDescriptorBuilder UseTopic(string topicPath)
    {
        TopicPath = topicPath;
        return this;
    }
    
    public ServiceBusConsumerDescriptorBuilder Configuration(Action<AzureServiceBusOptionsBase> configure)
    {
        configure(Options);
        return this;
    }
    
    public ServiceBusConsumerDescriptorBuilder UseDefaultOptions()
    {
        DefaultOptions = true;
        return this;
    }
    
    public KeyValuePair<string, IServiceBusConsumerDescriptor> Build(AzureServiceBusOptionsBase defaultOptions)
    {
        var options = DefaultOptions ? defaultOptions : Options;
        
        return new KeyValuePair<string, IServiceBusConsumerDescriptor> (
            GroupName, 
            new ServiceBusConsumerDescriptor(TopicPath, options));
    }
}

// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using DotNetCore.CAP.AzureServiceBus.Producer;

namespace DotNetCore.CAP.AzureServiceBus.Consumer;

public class ServiceBusConsumerDescriptorBuilder
{
    private string GroupName { get; set; } = null!;
    private string? Namespace { get; set; } = null;
    private string TopicPath { get; set; } = null!;
    
    
    public ServiceBusConsumerDescriptorBuilder UseGroupName(string groupName)
    {
        GroupName = groupName;
        return this;
    }

    public ServiceBusConsumerDescriptorBuilder UseTopic(string topicPath)
    {
        TopicPath = topicPath;
        return this;
    }
    
    public ServiceBusConsumerDescriptorBuilder UseNamespace(string @namespace)
    {
        Namespace = @namespace;
        return this;
    }
    
    public KeyValuePair<string, IServiceBusConsumerDescriptor> Build()
    {
        return new KeyValuePair<string, IServiceBusConsumerDescriptor> (GroupName, new ServiceBusConsumerDescriptor(TopicPath, Namespace));
    }
}

// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.AzureServiceBus.Consumer;

public interface IServiceBusConsumerDescriptor
{
    string TopicPath { get; }
    
     public AzureServiceBusOptionsBase Options { get; }
}

public class ServiceBusConsumerDescriptor : IServiceBusConsumerDescriptor
{
    public ServiceBusConsumerDescriptor(string topicPath, AzureServiceBusOptionsBase options)
    {
        TopicPath = topicPath;
        Options = options;
    }

    public string TopicPath { get; }
    
    public AzureServiceBusOptionsBase Options { get; }
}

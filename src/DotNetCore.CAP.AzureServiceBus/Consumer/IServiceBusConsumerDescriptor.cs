// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.AzureServiceBus.Consumer;

public interface IServiceBusConsumerDescriptor
{
    string TopicPath { get; }
    string? Namespace { get; }
}

public class ServiceBusConsumerDescriptor : IServiceBusConsumerDescriptor
{
    public ServiceBusConsumerDescriptor(string topicPath, string? @namespace)
    {
        TopicPath = topicPath;
        Namespace = @namespace;
    }

    public string TopicPath { get; }
    public string? Namespace { get; }
}

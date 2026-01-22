// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public class ServiceBusProducerDescriptorBuilder<T>
{
    private string TopicPath { get; set; } = null!;
    private bool CreateSubscription { get; set; }
    private bool EnableSessions { get; set; }

    public ServiceBusProducerDescriptorBuilder<T> UseTopic(string topicPath)
    {
        TopicPath = topicPath;
        return this;
    }

    public ServiceBusProducerDescriptorBuilder<T> WithSubscription()
    {
        CreateSubscription = true;
        return this;
    }

    public ServiceBusProducerDescriptorBuilder<T> WithSessions()
    {
        EnableSessions = true;
        return this;
    }

    public ServiceBusProducerDescriptor<T> Build()
    {
        return new ServiceBusProducerDescriptor<T>(TopicPath, CreateSubscription, EnableSessions);
    }
}
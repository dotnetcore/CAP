// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IServiceBusProducerDescriptor
{
    string TopicPath { get; }
    string MessageTypeName { get; }
    bool CreateSubscription { get; }
}

public class ServiceBusProducerDescriptor : IServiceBusProducerDescriptor
{
    public ServiceBusProducerDescriptor(Type type, string topicPath, bool createSubscription = true)
    {
        MessageTypeName = type.Name;
        TopicPath = topicPath;
        CreateSubscription = createSubscription;
    }

    public ServiceBusProducerDescriptor(string typeName, string topicPath, bool createSubscription = true)
    {
        MessageTypeName = typeName;
        TopicPath = topicPath;
        CreateSubscription = createSubscription;
    }

    public string TopicPath { get; set; }

    public string MessageTypeName { get; }
    public bool CreateSubscription { get; internal set; }
}

public class ServiceBusProducerDescriptor<T> : ServiceBusProducerDescriptor
{
    public ServiceBusProducerDescriptor(string topicPath, bool createSubscription = true) : base(typeof(T), topicPath, createSubscription)
    {
    }
}
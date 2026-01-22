// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IServiceBusProducerDescriptor
{
    string TopicPath { get; }
    string MessageTypeName { get; }
    bool CreateSubscription { get; }
    bool EnableSessions { get; }
}

public class ServiceBusProducerDescriptor : IServiceBusProducerDescriptor
{
    public ServiceBusProducerDescriptor(Type type, string topicPath, bool createSubscription = true, bool enableSessions = false)
    {
        MessageTypeName = type.Name;
        TopicPath = topicPath;
        CreateSubscription = createSubscription;
        EnableSessions = enableSessions;
    }

    public ServiceBusProducerDescriptor(string typeName, string topicPath, bool createSubscription = true, bool enableSessions = false)
    {
        MessageTypeName = typeName;
        TopicPath = topicPath;
        CreateSubscription = createSubscription;
        EnableSessions = enableSessions;
    }

    public string TopicPath { get; set; }

    public string MessageTypeName { get; }
    public bool CreateSubscription { get; internal set; }
    public bool EnableSessions { get; internal set; }
}

public class ServiceBusProducerDescriptor<T> : ServiceBusProducerDescriptor
{
    public ServiceBusProducerDescriptor(string topicPath, bool createSubscription = true, bool enableSessions = false) : base(typeof(T), topicPath, createSubscription, enableSessions)
    {
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IAzureServiceBusProducer : IAzureServiceBusTopicOptions
{
    /// <summary>
    /// Type of the message that will be produced.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Full name of <see cref="MessageType"/>.
    /// </summary>
    string MessageTypeFullName { get; }
}

public class AzureServiceBusProducer : IAzureServiceBusProducer
{
    public AzureServiceBusProducer(Type type, string topicPath, bool enableSessions)
    {
        MessageType = type;
        TopicPath = topicPath;
        EnableSessions = enableSessions;
    }

    public string TopicPath { get; set; }
    public Type MessageType { get; }
    public string MessageTypeFullName => MessageType.FullName;
    public Func<Message, List<KeyValuePair<string, string>>>? CustomHeaders { get; set; }
    public bool EnableSessions { get; set; }
}

public class AzureServiceBusProducer<T> : AzureServiceBusProducer
{
    public AzureServiceBusProducer(
        string topicPath,
        bool enableSessions) : base(typeof(T), topicPath, enableSessions)
    {
    }
}


using System;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IAzureServiceBusProducer : IAzureServiceBusTopicOptions
{
    string MessageTypeName { get; }
}

public class AzureServiceBusProducer : IAzureServiceBusProducer
{
    public AzureServiceBusProducer(Type type, string topicPath, bool enableSessions)
    {
        MessageTypeName = type.Name;
        TopicPath = topicPath;
        EnableSessions = enableSessions;
    }
    
    public AzureServiceBusProducer(string typeName, string topicPath, bool enableSessions)
    {
        MessageTypeName = typeName;
        TopicPath = topicPath;
        EnableSessions = enableSessions;
    }

    public string TopicPath { get; set; }
    public string MessageTypeName { get; }
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


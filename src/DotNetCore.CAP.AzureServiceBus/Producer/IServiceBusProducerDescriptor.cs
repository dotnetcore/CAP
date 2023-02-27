using System;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IServiceBusProducerDescriptor
{
    string TopicPath { get; }
    string MessageTypeName { get; }
}

public class ServiceBusProducerDescriptor : IServiceBusProducerDescriptor
{
    public ServiceBusProducerDescriptor(Type type, string topicPath)
    {
        MessageTypeName = type.Name;
        TopicPath = topicPath;
    }

    public ServiceBusProducerDescriptor(string typeName, string topicPath)
    {
        MessageTypeName = typeName;
        TopicPath = topicPath;
    }

    public string TopicPath { get; set; }

    public string MessageTypeName { get; }
}

public class ServiceBusProducerDescriptor<T> : ServiceBusProducerDescriptor
{
    public ServiceBusProducerDescriptor(string topicPath) : base(typeof(T), topicPath)
    {
    }
}
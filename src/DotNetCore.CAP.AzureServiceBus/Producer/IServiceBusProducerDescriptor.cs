using System;

namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IServiceBusProducerDescriptor
{
    string TopicPath { get; }
    string MessageTypeName { get; }
}

public class ServiceBusProducerDescriptorDescriptor : IServiceBusProducerDescriptor
{
    public ServiceBusProducerDescriptorDescriptor(Type type, string topicPath)
    {
        MessageTypeName = type.Name;
        TopicPath = topicPath;
    }

    public ServiceBusProducerDescriptorDescriptor(string typeName, string topicPath)
    {
        MessageTypeName = typeName;
        TopicPath = topicPath;
    }

    public string TopicPath { get; set; }

    public string MessageTypeName { get; }
}

public class ServiceBusProducerDescriptorDescriptor<T> : ServiceBusProducerDescriptorDescriptor
{
    public ServiceBusProducerDescriptorDescriptor(string topicPath) : base(typeof(T), topicPath)
    {
    }
}
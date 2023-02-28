namespace DotNetCore.CAP.AzureServiceBus.Producer;

public class ServiceBusProducerDescriptorBuilder<T>
{
    private string TopicPath { get; set; }

    public ServiceBusProducerDescriptorBuilder<T> WithTopic(string topicPath)
    {
        TopicPath = topicPath;
        return this;
    }

    public ServiceBusProducerDescriptor<T> Build()
    {
        return new ServiceBusProducerDescriptor<T>(TopicPath);
    }
}
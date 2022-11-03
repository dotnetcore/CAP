namespace DotNetCore.CAP.AzureServiceBus.Producer;

public class ServiceBusProducerDescriptorBuilder<T>
{
    private string TopicPath { get; set; }

    public ServiceBusProducerDescriptorBuilder<T> WithTopic(string topicPath)
    {
        TopicPath = topicPath;
        return this;
    }

    public ServiceBusProducerDescriptorDescriptor<T> Build()
    {
        return new ServiceBusProducerDescriptorDescriptor<T>(TopicPath);
    }
}

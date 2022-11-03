namespace DotNetCore.CAP.AzureServiceBus.Producer;

public class ServiceBusProducerDescriptorBuilder<T>
{
    private string TopicPath { get; set; }
    private bool EnableSessions { get; set; }

    public ServiceBusProducerDescriptorBuilder<T> WithTopic(string topicPath)
    {
        TopicPath = topicPath;
        return this;
    }

    public ServiceBusProducerDescriptorBuilder<T> WithSessionEnabled(bool enabled = false)
    {
        EnableSessions = enabled;
        return this;
    }

    public ServiceBusProducerDescriptorDescriptor<T> Build()
    {
        return new ServiceBusProducerDescriptorDescriptor<T>(TopicPath, EnableSessions);
    }
}

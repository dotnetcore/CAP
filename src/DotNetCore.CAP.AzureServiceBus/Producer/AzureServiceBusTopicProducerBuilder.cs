namespace DotNetCore.CAP.AzureServiceBus.Producer;

public class AzureServiceBusTopicProducerBuilder<T>
{
    private string TopicPath { get; set; }
    private bool EnableSessions { get; set; }

    public AzureServiceBusTopicProducerBuilder<T> WithTopic(string topicPath)
    {
        TopicPath = topicPath;
        return this;
    }

    public AzureServiceBusTopicProducerBuilder<T> WithSessionEnabled(bool enabled = false)
    {
        EnableSessions = enabled;
        return this;
    }

    public AzureServiceBusProducer<T> Build()
    {
        return new AzureServiceBusProducer<T>(TopicPath, EnableSessions);
    }
}

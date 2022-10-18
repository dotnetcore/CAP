namespace DotNetCore.CAP.AzureServiceBus.Producer;

public class AzureServiceBusProducerBuilder<T>
{
    private string TopicPath { get; set; }
    private bool EnableSessions { get; set; }

    public AzureServiceBusProducerBuilder<T> WithTopic(string topicPath)
    {
        TopicPath = topicPath;
        return this;
    }

    public AzureServiceBusProducerBuilder<T> WithSessionEnabled(bool enabled = false)
    {
        EnableSessions = enabled;
        return this;
    }

    public AzureServiceBusProducer<T> Build()
    {
        return new AzureServiceBusProducer<T>(TopicPath, EnableSessions);
    }
}

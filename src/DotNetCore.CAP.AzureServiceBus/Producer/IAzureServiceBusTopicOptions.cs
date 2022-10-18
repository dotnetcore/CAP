namespace DotNetCore.CAP.AzureServiceBus.Producer;

public interface IAzureServiceBusTopicOptions
{
    /// <summary>
    /// The name of the topic relative to the service namespace base address.
    /// </summary>
    string TopicPath { get; set; }
}
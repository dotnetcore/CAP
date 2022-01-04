using DotNetCore.CAP;

namespace Sample.AzureServiceBus.InMemory;

public class SampleSubscriber : ICapSubscribe
{
    public record Message(string Content);
    
    [CapSubscribe("cap.sample.tests")]
    public void Handle(Message message)
    {
        Console.WriteLine($"Message {message.Content} received");
    }
}
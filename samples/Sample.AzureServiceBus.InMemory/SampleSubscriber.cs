using DotNetCore.CAP;
using Sample.AzureServiceBus.InMemory.Contracts.DomainEvents;
using Sample.AzureServiceBus.InMemory.Contracts.IntegrationEvents;

namespace Sample.AzureServiceBus.InMemory;

public class SampleSubscriber : ICapSubscribe
{
    public record Message(string Content);
    
    [CapSubscribe(nameof(EntityCreated))]
    public void Handle(Message message)
    {
        Console.WriteLine($"Message {message.Content} received");
    }
    
    [CapSubscribe(nameof(EntityCreatedForIntegration), Group = "test")]
    public void Handle(EntityCreatedForIntegration message)
    {
        Console.WriteLine($"Message {message.Id} received");
    }
}
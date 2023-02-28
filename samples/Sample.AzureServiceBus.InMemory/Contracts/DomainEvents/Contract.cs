namespace Sample.AzureServiceBus.InMemory.Contracts.DomainEvents;

public record EntityCreated(Guid Id);

public record EntityDeleted(Guid Id);

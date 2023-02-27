using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Sample.AzureServiceBus.InMemory;
using Sample.AzureServiceBus.InMemory.Contracts.DomainEvents;
using Sample.AzureServiceBus.InMemory.Contracts.IntegrationEvents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(l => l.AddConsole());

builder.Services.AddCap(c =>
{
    c.UseInMemoryStorage();
    c.UseAzureServiceBus(asb =>
    {
        asb.ConnectionString = builder.Configuration.GetConnectionString("AzureServiceBus")!;
        asb.CustomHeaders = message => new List<KeyValuePair<string, string>>()
        {
            new(DotNetCore.CAP.Messages.Headers.MessageId,
                SnowflakeId.Default().NextId().ToString()),
            new(DotNetCore.CAP.Messages.Headers.MessageName, message.Subject),
            new("IsFromSampleProject", "'true'")
        };
        asb.SQLFilters = new List<KeyValuePair<string, string>>() {
            new("IsFromSampleProjectFilter","IsFromSampleProject = 'true'")
        };
        
        asb.ConfigureCustomProducer<EntityCreatedForIntegration>(cfg => cfg.WithTopic("entity-created"));
        asb.ConfigureCustomProducer<EntityDeletedForIntegration>(cfg => cfg.WithTopic("entity-deleted"));
    });

    c.UseDashboard();
});

builder.Services.AddSingleton<SampleSubscriber>();

var app = builder.Build();

app.MapGet("/entity-created-for-integration", async (ICapPublisher capPublisher) =>
{
    var message = new EntityCreatedForIntegration(Guid.NewGuid());
    await capPublisher.PublishAsync(nameof(EntityCreatedForIntegration), message);
});

app.MapGet("/entity-deleted-for-integration", async (ICapPublisher capPublisher) =>
{
    var message = new EntityDeletedForIntegration(Guid.NewGuid());
    await capPublisher.PublishAsync(nameof(EntityDeletedForIntegration), message);
});

app.MapGet("/entity-created", async (ICapPublisher capPublisher) =>
{
    var message = new EntityCreated(Guid.NewGuid());
    await capPublisher.PublishAsync(nameof(EntityCreated), message);
});

app.MapGet("/entity-deleted", async (ICapPublisher capPublisher) =>
{
    var message = new EntityDeleted(Guid.NewGuid());
    await capPublisher.PublishAsync(nameof(EntityDeleted), message);
});

app.Run();
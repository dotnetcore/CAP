using DotNetCore.CAP.Internal;
using Sample.AzureServiceBus.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(l => l.AddConsole());

builder.Services.AddCap(c =>
{
    c.UseInMemoryStorage();
    c.UseAzureServiceBus(asb =>
    {
        asb.ConnectionString = builder.Configuration.GetConnectionString("AzureServiceBus");
        asb.CustomHeaders = message => new List<KeyValuePair<string, string>>()
        {
            new(DotNetCore.CAP.Messages.Headers.MessageId,
                SnowflakeId.Default().NextId().ToString()),
            new(DotNetCore.CAP.Messages.Headers.MessageName, message.Label)
        };
    });

    c.UseDashboard();
});

builder.Services.AddSingleton<SampleSubscriber>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
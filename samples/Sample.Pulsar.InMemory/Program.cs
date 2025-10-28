using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configure services
string pulsarUri = builder.Configuration.GetValue("AppSettings:PulsarUri", "pulsar://localhost:6650");

builder.Services.AddCap(x =>
{
    x.UseInMemoryStorage();
    x.UsePulsar(pulsarUri);
    x.UseDashboard();
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure middleware pipeline
app.UseRouting();
app.MapControllers();

app.Run();
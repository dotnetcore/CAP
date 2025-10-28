using Amazon;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddCap(x =>
{
    x.UseInMemoryStorage();
    x.UseAmazonSQS(RegionEndpoint.CNNorthWest1);
    x.UseDashboard();
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure middleware pipeline
app.UseRouting();
app.MapControllers();

app.Run();
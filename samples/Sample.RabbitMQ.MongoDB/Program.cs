using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<IMongoClient>(new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));

builder.Services.AddCap(x =>
{
    x.UseMongoDB(builder.Configuration.GetConnectionString("MongoDB"));
    x.UseRabbitMQ("");
    x.UseDashboard();
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure middleware pipeline
app.UseRouting();
app.MapControllers();

app.Run();

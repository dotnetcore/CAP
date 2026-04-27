using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sample.RabbitMQ.MySql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddCap(x =>
{
    x.UseMySql(AppDbContext.ConnectionString);
    x.UseRabbitMQ("localhost");
    x.UseDashboard();
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();
app.MapControllers();

app.Run();

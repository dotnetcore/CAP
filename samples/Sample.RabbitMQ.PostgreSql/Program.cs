using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sample.RabbitMQ.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddCap(x =>
{
    x.UsePostgreSql(opt =>
    {
        opt.ConnectionString = AppDbContext.ConnectionString;
        // Custom table names: PostgreSql uses a real schema object.
        opt.Schema = "shop";
        opt.PublishedTableName = "Orders";
        opt.ReceivedTableName = "OrderEvents";
        opt.LockTableName = "OrderLocks";
    });
    x.UseRabbitMQ("localhost");
    x.UseDashboard();
});

builder.Services.AddControllers();

var app = builder.Build();

using (var connection = new NpgsqlConnection(AppDbContext.ConnectionString))
{
    connection.Execute("CREATE TABLE IF NOT EXISTS test (id SERIAL PRIMARY KEY, name TEXT NOT NULL);");
}

// Configure the HTTP request pipeline
app.UseRouting();
app.MapControllers();

app.Run();


using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers();

builder.Services
    .AddEndpointsApiExplorer();
builder.Services
    .AddSwaggerGen();

builder.Services
    .AddCap(options =>
    {
        options.UseRedis(redis =>
        {
            redis.Configuration = ConfigurationOptions.Parse("redis-node-0:6379,password=cap");
            redis.OnConsumeError = context =>
            {
                throw new InvalidOperationException("");
            };
        });

        options.UseSqlServer("Server=db;Database=master;User=sa;Password=P@ssw0rd;Encrypt=False");

        options.UseDashboard();

    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

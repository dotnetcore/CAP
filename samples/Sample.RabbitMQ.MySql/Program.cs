using Sample.RabbitMQ.MySql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<AppDbContext>();

builder.Services.AddCap(x =>
{
    x.UseEntityFramework<AppDbContext>();
    x.UseRabbitMQ("localhost");
    x.UseDashboard();
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();
app.MapControllers();

app.Run();

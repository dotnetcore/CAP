using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Sample.RabbitMQ.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Configure services
// docker-compose -f test/docker-compose.yml up -d sqlserver
builder.Services.AddDbContext<AppDbContext>();

//builder.Services
//    .AddSingleton<IConsumerServiceSelector, TypedConsumerServiceSelector>()
//    .AddQueueHandlers(typeof(Program).Assembly);

// Initialize database schema
await using (var connection = new SqlConnection(AppDbContext.ConnectionString))
{
    await connection.ExecuteAsync("""
        IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[Persons]') AND type IN ('U'))
        	DROP TABLE [dbo].[Persons]

        CREATE TABLE [dbo].[Persons] (
          [Id] int  IDENTITY(1,1) NOT NULL,
          [Name] varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS  NULL,
          [Age] int  NULL,
          [CreateTime] datetime2(7) DEFAULT getdate() NULL
        )
        """);
}

builder.Services.AddCap(x =>
{
    x.UseEntityFramework<AppDbContext>(opt =>
    {
        // Custom table names: SqlServer uses a real schema object.
        opt.Schema = "shop";
        opt.PublishedTableName = "Orders";
        opt.ReceivedTableName = "OrderEvents";
        opt.LockTableName = "OrderLocks";
    });
    x.UseRabbitMQ("127.0.0.1");
    x.UseDashboard();

    //x.EnablePublishParallelSend = true;

    //x.FailedThresholdCallback = failed =>
    //{
    //    var logger = failed.ServiceProvider.GetRequiredService<ILogger<Program>>();
    //    logger.LogError($@"A message of type {failed.MessageType} failed after executing {x.FailedRetryCount} several times, 
    //        requiring manual troubleshooting. Message name: {failed.Message.GetName()}");
    //};
    //x.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure middleware pipeline
app.UseRouting();
app.MapControllers();

app.Run();

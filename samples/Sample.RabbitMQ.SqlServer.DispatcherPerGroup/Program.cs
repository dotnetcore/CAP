using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
#if DEBUG
                .WriteTo.Seq("http://localhost:5341")
#endif
                .CreateLogger();

            try
            {
                Log.Information("Starting host...");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.InnerException ?? ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true);
                })
                .UseSerilog((context, configuration) =>
                {
                    configuration.ReadFrom.Configuration(context.Configuration);
                }, true, true)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

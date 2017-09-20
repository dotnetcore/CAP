using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Sample.Kafka.SqlServer
{
    public class Program
    {

        //var config = new ConfigurationBuilder()
        //    .AddCommandLine(args)
        //    .AddEnvironmentVariables("ASPNETCORE_")
        //    .Build();

        //var host = new WebHostBuilder()
        //    .UseConfiguration(config)
        //    .UseKestrel()
        //    .UseContentRoot(Directory.GetCurrentDirectory())
        //    .UseIISIntegration()
        //    .UseStartup<Startup>()
        //    .Build();

        //host.Run();
        public static void Main(string[] args)
        { 
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

    }
}
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;

namespace Sample.RabbitMQ.MySql
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging((hostingContext, builder) =>
                {
                    hostingContext.HostingEnvironment.ConfigureNLog("nlog.config");
                })
                .UseNLog()
                .Build();
    }
}

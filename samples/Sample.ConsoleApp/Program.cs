using System;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sample.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var container = new ServiceCollection();

            container.AddLogging(x => x.AddConsole());
            container.AddCap(x =>
            {
                x.UseInMemoryStorage();
                x.UseZeroMQ(cfg =>
                {
                    cfg.HostName = "127.0.0.1";
                    cfg.SubPort = 5556;
                    cfg.PubPort = 5557;

                    cfg.Pattern = DotNetCore.CAP.ZeroMQ.NetMQPattern.PubSub;
                });
                //x.UseRabbitMQ(cfg =>
                //{
                //    cfg.HostName = "172.17.124.92";
                //    cfg.UserName = "guest";
                //    cfg.Password = "guest";
                //});
                x.UseDashboard();
            });

            container.AddSingleton<EventSubscriber>();

            var sp = container.BuildServiceProvider();

            sp.GetService<IBootstrapper>().BootstrapAsync(default);

            Console.ReadLine();
        }
    }
}
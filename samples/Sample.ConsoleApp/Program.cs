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
                //console app does not support dashboard

                x.UseMySql("<ConnectionString>");
                x.UseRabbitMQ(z =>
                {
                    z.HostName = "192.168.3.57";
                    z.UserName = "user";
                    z.Password = "wJ0p5gSs17";
                });
            });

            container.AddSingleton<EventSubscriber>();

            var sp = container.BuildServiceProvider();

            sp.GetService<IBootstrapper>().BootstrapAsync();

            Console.ReadLine();
        }
    }
}
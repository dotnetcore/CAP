using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

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
                /*
                //console app does not support dashboard

                x.UseMySql("Server=192.168.3.57;Port=3307;Database=captest;Uid=root;Pwd=123123;");
                x.UseRabbitMQ(z =>
                {
                    z.HostName = "192.168.3.57";
                    z.UserName = "user";
                    z.Password = "wJ0p5gSs17";
                    // z.ExChangeType = "x-delayed-message"; // 延迟队列
                });
                */

                //如果你使用的ADO.NET，根据数据库选择进行配置：
                x.UseMySql("Server=192.168.16.150;Port=3306;Database=order;Uid=uid;Pwd=pwd;Charset=utf8mb4");

                //CAP支持 RabbitMQ、Kafka、AzureServiceBus 等作为MQ，根据使用选择配置：
                x.UseRabbitMQ(cfg =>
                {
                    cfg.HostName = "192.168.16.150";
                    cfg.VirtualHost = "dev";
                    cfg.Port = 5672;
                    cfg.UserName = "dev";
                    cfg.Password = "password";
                    cfg.ExchangeName = "ex.delayed.message";
                    cfg.ExChangeType = "x-delayed-message"; // 延迟队列
                });
            });

            container.AddSingleton<EventSubscriber>();

            var sp = container.BuildServiceProvider();

            sp.GetService<IBootstrapper>().BootstrapAsync(default);

            Console.ReadLine();
        }
    }
}
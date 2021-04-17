using System;
using System.Collections.Generic;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyConsumerSelector
{
    class Program
    {
        private static bool _useCustomSelector = true;
        public static void Main(string[] args)
        {
            var container = new ServiceCollection();

            container.AddLogging(x => x.AddConsole());
            
            if (_useCustomSelector)
                container.AddSingleton<IConsumerServiceSelector, MyConsumerServiceSelector>();
            
            container.AddTransient<IMySubscribe, CustomInterfaceTypesClass>();
            container.AddTransient<ICapSubscribe, CapSubscriber>();
            
            container.AddCap(x =>
            {
                x.UseInMemoryStorage();
                x.UseRabbitMQ(z =>
                {
                    z.ExchangeName = "MyConsumerSelector";
                    z.HostName = "localhost";
                    z.UserName = "guest";
                    z.Password = "guest";
                    z.CustomHeaders = e => new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>(DotNetCore.CAP.Messages.Headers.MessageId, SnowflakeId.Default().NextId().ToString()),
                        new KeyValuePair<string, string>(DotNetCore.CAP.Messages.Headers.MessageName, e.RoutingKey)
                    };
                });
            });

            var sp = container.BuildServiceProvider();

            sp.GetService<IBootstrapper>().BootstrapAsync(default);

            Console.ReadLine();
        }
    }
}
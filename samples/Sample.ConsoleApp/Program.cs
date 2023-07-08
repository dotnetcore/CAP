using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Filter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Savorboard.CAP.InMemoryMessageQueue;

namespace Sample.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cts = new System.Threading.CancellationTokenSource();
            var container = new ServiceCollection();

            container.AddLogging(x => x.AddConsole());
            container.AddCap(x =>
            {
                //console app does not support dashboard

                x.UseInMemoryStorage();
                x.UseInMemoryMessageQueue();
            }).AddSubscribeFilter<Filter>();

            container.AddSingleton<EventSubscriber>();

            var sp = container.BuildServiceProvider();

            sp.GetService<IBootstrapper>().BootstrapAsync(cts.Token);

            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(2000, cts.Token);

                    await sp.GetService<ICapPublisher>().PublishAsync("sample.console.showtime", DateTime.Now, cancellationToken: cts.Token);
                }
            }, cts.Token);

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                cts.Cancel();
            };

            Console.ReadLine();
        }
    }

    public class Filter : SubscribeFilter
    {
        public override Task OnSubscribeExceptionAsync(ExceptionContext context)
        {
            if (context.Exception.InnerException is TimeoutException)
            {
                throw new TimeoutException("Http request timeout");
            }

            return base.OnSubscribeExceptionAsync(context);
        }
    }
}
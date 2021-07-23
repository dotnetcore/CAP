using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Test.Helpers;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Sdk;

namespace DotNetCore.CAP.Test.Nats
{
    public class NatsTransportTest
    {
        [Fact]
        public async Task TestSerializeFormator()
        {
            var service = new ServiceCollection();

            service.AddLogging(builder => { builder.AddTestLogging(new TestOutputHelper()); });
            service.AddTransient<ITestSubscriber, TestSubscriber>();
            service.AddCap(x =>
            {
                x.UseInMemoryStorage();
                x.UseNATS(opt => { opt.Servers = "127.0.0.1:4222"; });
            });

            var provider = service.BuildServiceProvider();

            var bootstrapper = provider.GetRequiredService<IBootstrapper>();
            bootstrapper.BootstrapAsync();

            var publisher = provider.GetRequiredService<ICapPublisher>();
            publisher.Publish("testname", "testcontent");

            System.Threading.Thread.Sleep(10000);
        }
    }

    public interface ITestSubscriber
    {
        void Subscribe(string cotnent);
    }

    public class TestSubscriber : ICapSubscribe, ITestSubscriber
    {
        [CapSubscribe("testname")]
        public void Subscribe(string cotnent)
        {
            Console.WriteLine(cotnent);
        }
    }
}
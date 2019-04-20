using System;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DotNetCore.CAP.CastleCoreTest
{
    public class ConsumerServiceSelectorTest
    {
        private readonly IServiceProvider _provider;

        public ConsumerServiceSelectorTest()
        {
            var services = new ServiceCollection();
            
            services.AddSingleton(typeof(ICapSubscribe), f =>
            {
                var generator = new ProxyGenerator();
                return generator.CreateClassProxy(typeof(TestSubscribeClass));
            });

            services.AddSingleton<ITestSubscribeClass, TestSubscribeClass>();

            services.AddLogging();

            services.TryAddSingleton<IConsumerServiceSelector, CastleCoreConsumerServiceSelector>();

            services.AddCap(x => { });

            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanFindCapSubscribeTopic()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();
            Assert.Equal(1, candidates.Count);
        }
    }

    public interface ITestSubscribeClass
    {

    }

    public class TestSubscribeClass : ITestSubscribeClass, ICapSubscribe
    {
        [CapSubscribe("cap.castle.sub")]
        public void TestSubscribe(DateTime dateTime)
        {
            Console.WriteLine(dateTime);
        }
    }
}

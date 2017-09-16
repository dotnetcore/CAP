using System;
using DotNetCore.CAP.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class SubscribeFinderTest
    {
        private IServiceProvider _provider;

        public SubscribeFinderTest()
        {
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();
            services.AddCap(x => { });
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanFindControllers()
        {
        }

        [Fact]
        public void CanFindSubscribeService()
        {
            var testService = _provider.GetService<ICapSubscribe>();
            Assert.NotNull(testService);
            Assert.IsType<TestService>(testService);
        }
    }

    public class HomeController
    {
    }

    public interface ITestService { }

    public class TestService : ITestService, ICapSubscribe
    {
        [CapSubscribe("test")]
        public void Index()
        {
        }
    }

    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string name) : base(name)
        {
        }
    }
}
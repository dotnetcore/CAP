using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class CapBuilderTest
    {

        [Fact]
        public void CanCreateInstanceAndGetService()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ICapPublisher, MyProducerService>();
            var builder = new CapBuilder(services);
            Assert.NotNull(builder);

            var count = builder.Services.Count;
            Assert.Equal(1, count);

            var provider = services.BuildServiceProvider();
            var capPublisher = provider.GetService<ICapPublisher>();
            Assert.NotNull(capPublisher);
        }

        [Fact]
        public void CanAddCapService()
        {
            var services = new ServiceCollection();
            services.AddCap(x => { });
            var builder = services.BuildServiceProvider();

            var markService = builder.GetService<CapMarkerService>();
            Assert.NotNull(markService);
        }

        [Fact]
        public void CanOverridePublishService()
        {
            var services = new ServiceCollection();
            services.AddCap(x => { }).AddProducerService<MyProducerService>();

            var thingy = services.BuildServiceProvider()
                .GetRequiredService<ICapPublisher>() as MyProducerService;

            Assert.NotNull(thingy);
        }
      

        [Fact]
        public void CanResolveCapOptions()
        {
            var services = new ServiceCollection();
            services.AddCap(x => { });
            var builder = services.BuildServiceProvider();
            var capOptions = builder.GetService<IOptions<CapOptions>>().Value;
            Assert.NotNull(capOptions);
        }

        private class MyProducerService : ICapPublisher
        {
            public IServiceProvider ServiceProvider { get; }

            public AsyncLocal<ICapTransaction> Transaction { get; }

            public void ManuallySendMq()
            {
                throw new NotImplementedException();
            }

            public void Publish<T>(string name, [global::JetBrains.Annotations.CanBeNullAttribute] T contentObj, string callbackName = null, bool manuallySendMq = false)
            {
                throw new NotImplementedException();
            }

            public void Publish<T>(string name, [global::JetBrains.Annotations.CanBeNullAttribute] T contentObj, IDictionary<string, string> headers, bool manuallySendMq = false)
            {
                throw new NotImplementedException();
            }

            public Task PublishAsync<T>(string name, [global::JetBrains.Annotations.CanBeNullAttribute] T contentObj, string callbackName = null, bool manuallySendMq = false, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task PublishAsync<T>(string name, [global::JetBrains.Annotations.CanBeNullAttribute] T contentObj, IDictionary<string, string> headers, bool manuallySendMq = false, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
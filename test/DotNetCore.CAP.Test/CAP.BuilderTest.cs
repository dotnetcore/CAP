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

            public Task PublishAsync<T>(string name, T contentObj, string callbackName = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task PublishAsync<T>(string name, T contentObj, IDictionary<string, string> optionHeaders = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public void Publish<T>(string name, T contentObj, string callbackName = null)
            {
                throw new NotImplementedException();
            }

            public void Publish<T>(string name, T contentObj, IDictionary<string, string> headers)
            {
                throw new NotImplementedException();
            }
        }
    }
}
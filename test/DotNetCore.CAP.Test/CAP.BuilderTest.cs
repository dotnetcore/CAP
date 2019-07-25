using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
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
        public void CanOverrideContentSerialize()
        {
            var services = new ServiceCollection();
            services.AddCap(x => { }).AddContentSerializer<MyContentSerializer>();

            var thingy = services.BuildServiceProvider()
                .GetRequiredService<IContentSerializer>() as MyContentSerializer;

            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideMessagePack()
        {
            var services = new ServiceCollection();
            services.AddCap(x => { }).AddMessagePacker<MyMessagePacker>();

            var thingy = services.BuildServiceProvider()
                .GetRequiredService<IMessagePacker>() as MyMessagePacker;

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

        private class MyMessagePacker : IMessagePacker
        {
            public string Pack(CapMessage obj)
            {
                throw new NotImplementedException();
            }

            public CapMessage UnPack(string packingMessage)
            {
                throw new NotImplementedException();
            }
        }


        private class MyContentSerializer : IContentSerializer
        {
            public T DeSerialize<T>(string content)
            {
                throw new NotImplementedException();
            }

            public object DeSerialize(string content, Type type)
            {
                throw new NotImplementedException();
            }

            public string Serialize<T>(T obj)
            {
                throw new NotImplementedException();
            }
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

            public void Publish<T>(string name, T contentObj, string callbackName = null)
            {
                throw new NotImplementedException();
            }
        }
    }
}
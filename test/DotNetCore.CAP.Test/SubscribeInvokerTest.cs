using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class SubscribeInvokerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public SubscribeInvokerTest()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ISerializer, JsonUtf8Serializer>();
            serviceCollection.AddSingleton<ISubscribeInvoker, SubscribeInvoker>();
            serviceCollection.AddSingleton<ISnowflakeId>(r => new SnowflakeId(Util.GenerateWorkerId(1023)));
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private ISubscribeInvoker SubscribeInvoker => _serviceProvider.GetService<ISubscribeInvoker>();

        [Fact]
        public async Task InvokeTest()
        {
            var snowflakeId = _serviceProvider.GetRequiredService<ISnowflakeId>();
            var descriptor = new ConsumerExecutorDescriptor()
            {
                Attribute = new CandidatesTopic("fake.output.integer"),
                ServiceTypeInfo = typeof(FakeSubscriber).GetTypeInfo(),
                ImplTypeInfo = typeof(FakeSubscriber).GetTypeInfo(),
                MethodInfo = typeof(FakeSubscriber).GetMethod(nameof(FakeSubscriber.OutputIntegerSub)),
                Parameters = new List<ParameterDescriptor>()
            };

            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = snowflakeId.NextId().ToString(),
                [Headers.MessageName] = "fake.output.integer"
            };
            var message = new Message(header, null);
            var mediumMessage = new MediumMessage() { Origin = message };
            var context = new ConsumerContext(descriptor, mediumMessage);

            var ret = await SubscribeInvoker.InvokeAsync(context);
            Assert.Equal(int.MaxValue, ret.Result);
        }
    }

    public class FakeSubscriber : ICapSubscribe
    {
        [CapSubscribe("fake.output.integer")]
        public int OutputIntegerSub()
        {
            return int.MaxValue;
        }
    }
}

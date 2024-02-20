using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class SubscribeInvokerWithCancellation
    {
        private readonly IServiceProvider _serviceProvider;

        public SubscribeInvokerWithCancellation()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<IBootstrapper, Bootstrapper>();
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
                Attribute = new CandidatesTopic("fake.output.withcancellation"),
                ServiceTypeInfo = typeof(FakeSubscriberWithCancellation).GetTypeInfo(),
                ImplTypeInfo = typeof(FakeSubscriberWithCancellation).GetTypeInfo(),
                MethodInfo = typeof(FakeSubscriberWithCancellation)
                    .GetMethod(nameof(FakeSubscriberWithCancellation.CancellationTokenInjected)),
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor {
                        ParameterType = typeof(CancellationToken),
                        IsFromCap = true,
                        Name = "cancellationToken"
                    }
                }
            };

            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = snowflakeId.NextId().ToString(),
                [Headers.MessageName] = "fake.output.withcancellation"
            };
            var message = new Message(header, null);
            var mediumMessage = new MediumMessage() { Origin = message };
            var context = new ConsumerContext(descriptor, mediumMessage);

            var cancellationToken = new CancellationToken();
            var ret = await SubscribeInvoker.InvokeAsync(context, cancellationToken);
            Assert.Equal(cancellationToken, ret.Result);
        }
    }

    public class FakeSubscriberWithCancellation : ICapSubscribe
    {
        [CapSubscribe("fake.output.withcancellation")]
        public CancellationToken CancellationTokenInjected(CancellationToken cancellationToken)
        {
            return cancellationToken;
        }
    }
}

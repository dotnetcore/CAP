using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ConsumerInvokerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public ConsumerInvokerTest()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<IConsumerInvoker, DefaultConsumerInvoker>();
            serviceCollection.AddTransient<FakeSubscriber>();
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private IConsumerInvoker ConsumerInvoker => _serviceProvider.GetService<IConsumerInvoker>();

        [Fact]
        public async Task InvokeTest()
        {
            var descriptor = new ConsumerExecutorDescriptor()
            {
                Attribute = new CandidatesTopic("fake.output.integer"),
                ServiceTypeInfo = typeof(FakeSubscriber).GetTypeInfo(),
                ImplTypeInfo = typeof(FakeSubscriber).GetTypeInfo(),
                MethodInfo = typeof(FakeSubscriber).GetMethod(nameof(FakeSubscriber.OutputIntegerSub)),
                Parameters = new List<ParameterDescriptor>()
            };

            var header = new Dictionary<string, string>();
            var message = new Message(header, null);
            var context = new ConsumerContext(descriptor, message);

            var ret = await ConsumerInvoker.InvokeAsync(context);
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

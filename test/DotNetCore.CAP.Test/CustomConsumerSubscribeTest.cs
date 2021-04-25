using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class CustomConsumerSubscribeTest
    {
        private const string TopicNamePrefix = "topic";
        private const string GroupNamePrefix = "group";

        private readonly IServiceProvider _provider;

        public CustomConsumerSubscribeTest()
        {
            var services = new ServiceCollection();
            // Declare subscribe interface and attribute when configuring services.
            services.AddSingleton<IConsumerServiceSelector, GenericConsumerServiceSelector<IMySubscribe, MySubscribeAttribute>>();
            services.AddTransient<IMySubscribe, CustomInterfaceTypesClass>();
            services.AddLogging();
            services.AddCap(x =>
            {
                x.TopicNamePrefix = TopicNamePrefix;
                x.GroupNamePrefix = GroupNamePrefix;
            });
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanFindAllConsumerService()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            Assert.Equal(3, candidates.Count);
        }

        [Fact]
        public void CanFindSpecifiedTopic()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();
            var bestCandidates = selector.SelectBestCandidate($"{TopicNamePrefix}.Candidates.Foo", candidates);

            Assert.NotNull(bestCandidates);
            Assert.NotNull(bestCandidates.MethodInfo);
            Assert.StartsWith(GroupNamePrefix, bestCandidates.Attribute.Group);
            Assert.StartsWith(TopicNamePrefix, bestCandidates.TopicName);
            Assert.Equal(typeof(Task), bestCandidates.MethodInfo.ReturnType);
        }
        
        [Fact]
        public void CanFindTopicWithParameters()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();
            var bestCandidates = selector.SelectBestCandidate($"{TopicNamePrefix}.Candidates.Foo3", candidates);

            Assert.NotNull(bestCandidates);
            Assert.NotNull(bestCandidates.Parameters);
            Assert.StartsWith(GroupNamePrefix, bestCandidates.Attribute.Group);
            Assert.StartsWith(TopicNamePrefix, bestCandidates.TopicName);
            Assert.Equal(typeof(Task), bestCandidates.MethodInfo.ReturnType);
        }
    }

    public interface IMySubscribe { }

    public class MySubscribeAttribute : Attribute, INamedGroup
    {
        public MySubscribeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Group { get; set; }
    }

    public class CustomInterfaceTypesClass : IMySubscribe
    {
        [MySubscribe("Candidates.Foo")]
        public Task GetFoo()
        {
            Console.WriteLine("GetFoo() method has been executed.");
            return Task.CompletedTask;
        }

        [MySubscribe("Candidates.Foo2")]
        public void GetFoo2()
        {
            Console.WriteLine("GetFoo2() method has been executed.");
        }
        
        [MySubscribe("Candidates.Foo3")]
        public Task GetFoo3(string message)
        {
            Console.WriteLine($"GetFoo3() received message {message}.");
            return Task.CompletedTask;
        }

        public void DistracterMethod()
        {
            Console.WriteLine("DistracterMethod() method has been executed.");
        }
    }
}
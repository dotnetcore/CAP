using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ConsumerServiceSelectorTest
    {
        private readonly IServiceProvider _provider;

        public ConsumerServiceSelectorTest()
        {
            var services = new ServiceCollection(); 
            ServiceCollectionExtensions.ServiceCollection = services;
            services.AddOptions();
            services.PostConfigure<CapOptions>(x=>{});
            services.AddSingleton<IConsumerServiceSelector, DefaultConsumerServiceSelector>();
            services.AddScoped<IFooTest, CandidatesFooTest>();
            services.AddScoped<IBarTest, CandidatesBarTest>();
            services.AddLogging();
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanFindAllConsumerService()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            Assert.Equal(6, candidates.Count);
        }

        [Fact]
        public void CanFindSpecifiedTopic()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();
            var bestCandidates = selector.SelectBestCandidate("Candidates.Foo", candidates);

            Assert.NotNull(bestCandidates);
            Assert.NotNull(bestCandidates.MethodInfo);
            Assert.Equal(typeof(Task), bestCandidates.MethodInfo.ReturnType);
        }


        [Theory]
        [InlineData("Candidates.Asterisk")]
        [InlineData("candidates.Asterisk")]
        [InlineData("AAA.BBB.Asterisk")]
        [InlineData("aaa.bbb.Asterisk")]
        public void CanFindAsteriskTopic(string topic)
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            var bestCandidates = selector.SelectBestCandidate(topic, candidates);
            Assert.NotNull(bestCandidates);
        }

        [Theory]
        [InlineData("Candidates.Asterisk.AAA")]
        [InlineData("AAA.BBB.CCC.Asterisk")]
        [InlineData("aaa.BBB.ccc.Asterisk")]
        [InlineData("Asterisk.aaa.bbb")]
        public void CanNotFindAsteriskTopic(string topic)
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            var bestCandidates = selector.SelectBestCandidate(topic, candidates);
            Assert.Null(bestCandidates);
        }

        [Theory]
        [InlineData("Candidates.Pound.AAA")]
        [InlineData("Candidates.Pound.AAA.BBB")]
        [InlineData("AAA.Pound")]
        [InlineData("aaa.Pound")]
        [InlineData("aaa.bbb.Pound")]
        [InlineData("aaa.BBB.Pound")]
        public void CanFindPoundTopic(string topic)
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            var bestCandidates = selector.SelectBestCandidate(topic, candidates);
            Assert.NotNull(bestCandidates);
        }

        [Theory]
        [InlineData("Pound")]
        [InlineData("aaa.Pound.AAA.BBB")]
        [InlineData("Pound.AAA")]
        [InlineData("Pound.aaa")]
        [InlineData("AAA.Pound.aaa")]
        public void CanNotFindPoundTopic(string topic)
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            var bestCandidates = selector.SelectBestCandidate(topic, candidates);
            Assert.Null(bestCandidates);
        }
    }

    public class CandidatesTopic : TopicAttribute
    {
        public CandidatesTopic(string topicName) : base(topicName)
        {
        }
    }

    public interface IFooTest
    {
    }

    public interface IBarTest
    {
    }

    public class CandidatesFooTest : IFooTest, ICapSubscribe
    {
        [CandidatesTopic("Candidates.Foo")]
        public Task GetFoo()
        {
            Console.WriteLine("GetFoo() method has bee excuted.");
            return Task.CompletedTask;
        }

        [CandidatesTopic("Candidates.Foo2")]
        public void GetFoo2()
        {
            Console.WriteLine("GetFoo2() method has bee excuted.");
        }

        [CandidatesTopic("*.*.Asterisk")]
        [CandidatesTopic("*.Asterisk")]
        public void GetFooAsterisk()
        {
            Console.WriteLine("GetFoo2Asterisk() method has bee excuted.");
        }

        [CandidatesTopic("Candidates.Pound.#")]
        [CandidatesTopic("#.Pound")]
        public void GetFooPound()
        {
            Console.WriteLine("GetFoo2Pound() method has bee excuted.");
        }

    }

    public class CandidatesBarTest : IBarTest
    {
        [CandidatesTopic("Candidates.Bar")]
        public Task GetBar()
        {
            Console.WriteLine("GetBar() method has bee excuted.");
            return Task.CompletedTask;
        }

        [CandidatesTopic("Candidates.Bar2")]
        public void GetBar2()
        {
            Console.WriteLine("GetBar2() method has bee excuted.");
        }

        public void GetBar3()
        {
            Console.WriteLine("GetBar3() method has bee excuted.");
        }
    }
}
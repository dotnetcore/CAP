using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ConsumerServiceSelectorTest
    {
        private IServiceProvider _provider;

        public ConsumerServiceSelectorTest()
        {
            var services = new ServiceCollection();
            //services.AddSingleton<IConsumerServiceSelector, DefaultConsumerServiceSelector>();
            services.AddScoped<IFooTest, CandidatesFooTest>();
            services.AddScoped<IBarTest, CandidatesBarTest>();
            services.AddLogging();
            services.AddCap(x => { });
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanFindAllConsumerService()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            Assert.Equal(2, candidates.Count);
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
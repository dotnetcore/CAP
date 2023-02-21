using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
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
            services.AddOptions();
            services.PostConfigure<CapOptions>(x=>{});
            services.AddSingleton<IServiceCollection>(_ => services);
            services.AddSingleton<IConsumerServiceSelector, ConsumerServiceSelector>();
            services.AddScoped<IFooTest, CandidatesFooTest>();			
            services.AddScoped<IBarTest, CandidatesBarTest>();
			services.AddScoped<IAbstractTest, CandidatesAbstractTest>();
			services.AddScoped<ICancellationTest, CancellationTokenTest>();
            services.AddLogging();
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanFindAllConsumerService()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();
            
            Assert.Equal(13, candidates.Count);
        }

        [Theory]
        [InlineData("Candidates.Foo")]
        [InlineData("Candidates.Foo3")]
        [InlineData("Candidates.Foo4")]
        public void CanFindSpecifiedTopic(string topic)
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();
            var bestCandidates = selector.SelectBestCandidate(topic, candidates);

            Assert.NotNull(bestCandidates);
            Assert.NotNull(bestCandidates.MethodInfo);
            Assert.Equal(typeof(Task), bestCandidates.MethodInfo.ReturnType);
        }
		
		[Theory]
		[InlineData("Candidates.Abstract")]
		[InlineData("Candidates.Abstract2")]
		public void CanFindInheritedMethodsTopic(string topic)
		{
			var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
			var candidates = selector.SelectCandidates();
			var bestCandidates = selector.SelectBestCandidate(topic, candidates);

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
        public void CanNotFindAsteriskTopic(string topic)
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            var bestCandidates = selector.SelectBestCandidate(topic, candidates);
            Assert.Null(bestCandidates);
        }

        [Theory]
        [InlineData("Asterisk.aaa.bbb")]
        public void CanNotFindAsteriskTopic2(string topic)
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

        [Theory]
        [InlineData("CancellationToken.NoAdditionalParameters", 1, 1)]
        [InlineData("CancellationToken.OneAdditionalParameter", 2, 2)]
        [InlineData("CancellationToken.CommonArrangement", 3, 2)]
        public void CanFindTopicSuccessfully(string topic, int parameterCount, int fromCapCount)
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            var bestCandidate = selector.SelectBestCandidate(topic, candidates);
            Assert.NotEqual(0, candidates.Count);
            Assert.NotNull(bestCandidate);
            Assert.Equal(bestCandidate.Parameters.Count, parameterCount);
            Assert.Equal(fromCapCount, bestCandidate.Parameters.Count(x => x.IsFromCap));
        }
    }

    public class CandidatesTopic : TopicAttribute
    {
        public CandidatesTopic(string topicName, bool isPartial = false) : base(topicName, isPartial)
        {
        }
    }

    public interface IFooTest
    {
    }

    public interface IBarTest
    {
    }

    public interface IAbstractTest
    {
    }

    public interface ICancellationTest
    {
    }

    [CandidatesTopic("Candidates")]
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

        [CandidatesTopic("Foo3", isPartial: true)]
        public Task GetFoo3()
        {
            Console.WriteLine("GetFoo3() method has bee excuted.");
            return Task.CompletedTask;
        }

        [CandidatesTopic(".Foo4", isPartial: true)]
        public Task GetFoo4()
        {
            Console.WriteLine("GetFoo4() method has bee excuted.");
            return Task.CompletedTask;
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
	
	/// <summary>
	/// Test to verify if an inherited class also gets the subscribed methods.
	/// Abstract class doesn't have a subscribe topic, inherited class with a topic
	/// should also get the partial subscribed methods.
	/// </summary>
	public abstract class CandidatesAbstractBaseTest : ICapSubscribe, IAbstractTest
	{
		[CandidatesTopic("Candidates.Abstract")]
		public virtual Task GetAbstract()
		{
		  Console.WriteLine("GetAbstract() method has been excuted.");
		  return Task.CompletedTask;
		}

		[CandidatesTopic("Abstract2", isPartial: true)]
		public virtual Task GetAbstract2()
		{
		  Console.WriteLine("GetAbstract2() method has been excuted.");
		  return Task.CompletedTask;
		}
	}

	[CandidatesTopic("Candidates")]
	public class CandidatesAbstractTest : CandidatesAbstractBaseTest
	{
	}
    
    [CandidatesTopic("Candidates")]
    public class CancellationTokenTest : ICancellationTest, ICapSubscribe
    {
        [CandidatesTopic("CancellationToken.NoAdditionalParameters")]
        public Task NoAdditionalParameters(CancellationToken cancellationToken)
        {
            Console.WriteLine($"{nameof(NoAdditionalParameters)} method has been executed.");
            return Task.CompletedTask;
        }

        [CandidatesTopic("CancellationToken.OneAdditionalParameter")]
        public Task OneAdditionalParameter([FromCap] CapHeader headers, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{nameof(OneAdditionalParameter)} method has been executed.");
            return Task.CompletedTask;
        }

        [CandidatesTopic("CancellationToken.CommonArrangement")]
        public Task CommonArrangement(DateTime date, [FromCap] IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{nameof(CommonArrangement)} method has been executed.");
            return Task.CompletedTask;
        }
    }
}
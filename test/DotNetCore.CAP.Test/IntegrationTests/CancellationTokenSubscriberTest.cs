using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace DotNetCore.CAP.Test.IntegrationTests
{
    public class CancellationTokenSubscriberTest : IntegrationTestBase
    {
        public CancellationTokenSubscriberTest(ITestOutputHelper testOutput)
            : base(testOutput)
        {
        }

        [Fact]
        public async Task Execute()
        {
            await Publisher.PublishAsync(nameof(CancellationTokenSubscriberTest), "Test Message");
            await HandledMessages.WaitOneMessage(CancellationToken);

            // Explicitly stop Bootstrapper to prove the cancellation token works.
            var bootstrapper = Container.GetRequiredService<Bootstrapper>();
       
            await bootstrapper.StopAsync(CancellationToken.None);

            var (message, token) = HandledMessages
                .OfType<(string Message, CancellationToken Token)>()
                .Single();

            Assert.Equal("Test Message", message);
            Assert.True(token.IsCancellationRequested);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<TestEventSubscriber>();
        }

        private class TestEventSubscriber : ICapSubscribe
        {
            private readonly TestMessageCollector _collector;
            private readonly ILogger<TestEventSubscriber> _logger;

            public TestEventSubscriber(ILogger<TestEventSubscriber> logger, TestMessageCollector collector)
            {
                _logger = logger;
                _collector = collector;
            }

            [CapSubscribe(nameof(CancellationTokenSubscriberTest),
                Group = TestServiceCollectionExtensions.TestGroupName)]
            public void Handle(string message, CancellationToken cancellationToken)
            {
                _logger.LogWarning($"{nameof(Handle)} method called. {message}");
                _collector.Add((message, cancellationToken));
            }
        }
    }
}
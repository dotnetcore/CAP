using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;

namespace Cap.Consistency.Test
{
    public class ConsistencyMessageManagerTest
    {
        [Fact]
        public void EnsureDefaultServicesDefaultsWithStoreWorks() {
            var services = new ServiceCollection()
                .AddTransient<IConsistencyMessageStore<TestConsistencyMessage>, NoopMessageStore>();
            services.AddConsistency<TestConsistencyMessage>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddLogging();

            var manager = services.BuildServiceProvider()
                .GetRequiredService<ConsistencyMessageManager<TestConsistencyMessage>>();

            Assert.NotNull(manager);
        }

        [Fact]
        public void AddMessageManagerWithCustomerMannagerReturnsSameInstance() {
            var services = new ServiceCollection()
                .AddTransient<IConsistencyMessageStore<TestConsistencyMessage>, NoopMessageStore>()
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddLogging();

            services.AddConsistency<TestConsistencyMessage>()
                .AddConsistencyMessageManager<CustomMessageManager>();

            var provider = services.BuildServiceProvider();

            Assert.Same(provider.GetRequiredService<ConsistencyMessageManager<TestConsistencyMessage>>(),
                provider.GetRequiredService<CustomMessageManager>());
        }

        public class CustomMessageManager : ConsistencyMessageManager<TestConsistencyMessage>
        {
            public CustomMessageManager()
                : base(new Mock<IConsistencyMessageStore<TestConsistencyMessage>>().Object, null, null) {
            }
        }

        [Fact]
        public async Task CreateCallsStore() {
            var store = new Mock<IConsistencyMessageStore<TestConsistencyMessage>>();
            var message = new TestConsistencyMessage { Time = DateTime.Now };
            store.Setup(x => x.CreateAsync(message, CancellationToken.None)).ReturnsAsync(OperateResult.Success).Verifiable();
            var messageManager = TestConsistencyMessageManager(store.Object);

            var result = await messageManager.CreateAsync(message);

            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        public ConsistencyMessageManager<TMessage> TestConsistencyMessageManager<TMessage>(IConsistencyMessageStore<TMessage> store = null)
            where TMessage : class {
            store = store ?? new Mock<IConsistencyMessageStore<TMessage>>().Object;
            var mockLogger = new Mock<ILogger<ConsistencyMessageManager<TMessage>>>().Object;
            var manager = new ConsistencyMessageManager<TMessage>(store, null, mockLogger);
            return manager;
        }
    }
}

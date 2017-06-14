using System;
using System.Threading;
using System.Threading.Tasks;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cap.Consistency.Test
{
    public class ConsistencyMessageManagerTest
    {
        [Fact]
        public void EnsureDefaultServicesDefaultsWithStoreWorks() {
            var services = new ServiceCollection()
                .AddTransient<IConsistencyMessageStore, NoopMessageStore>();
            services.AddConsistency();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddLogging();

            var manager = services.BuildServiceProvider()
                .GetRequiredService<ConsistencyMessageManager>();

            Assert.NotNull(manager);
        }

        [Fact]
        public void AddMessageManagerWithCustomerMannagerReturnsSameInstance() {
            var services = new ServiceCollection()
                .AddTransient<IConsistencyMessageStore, NoopMessageStore>()
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddLogging();

            //services.AddConsistency()
            //    .AddConsistencyMessageManager<CustomMessageManager>();

            var provider = services.BuildServiceProvider();

            Assert.Same(provider.GetRequiredService<ConsistencyMessageManager>(),
                provider.GetRequiredService<CustomMessageManager>());
        }

        public class CustomMessageManager : ConsistencyMessageManager
        {
            public CustomMessageManager()
                : base(new Mock<IConsistencyMessageStore>().Object, null, null) {
            }
        }

        [Fact]
        public async Task CreateCallsStore() {
            var store = new Mock<IConsistencyMessageStore>();
            var message = new ConsistencyMessage { SendTime = DateTime.Now };
            store.Setup(x => x.CreateAsync(message, CancellationToken.None)).ReturnsAsync(OperateResult.Success).Verifiable();
            var messageManager = TestConsistencyMessageManager(store.Object);

            var result = await messageManager.CreateAsync(message);

            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        public ConsistencyMessageManager TestConsistencyMessageManager(IConsistencyMessageStore store = null) {
            store = store ?? new Mock<IConsistencyMessageStore>().Object;
            var mockLogger = new Mock<ILogger<ConsistencyMessageManager>>().Object;
            var manager = new ConsistencyMessageManager(store, null, mockLogger);
            return manager;
        }
    }
}
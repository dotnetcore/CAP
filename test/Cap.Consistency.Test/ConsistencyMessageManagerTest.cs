using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

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
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cap.Consistency.Test
{
    public class ConsistencyBuilderTest
    {
        [Fact]
        public void CanOverrideMessageStore() {
            var services = new ServiceCollection();
            services.AddConsistency<TestConsistencyMessage>().AddMessageStore<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IConsistencyMessageStore<TestConsistencyMessage>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        private class MyUberThingy : IConsistencyMessageStore<TestConsistencyMessage>
        {
            public Task<OperateResult> CreateAsync(TestConsistencyMessage message, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }

            public Task<OperateResult> DeleteAsync(TestConsistencyMessage message, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }

            public void Dispose() {
                throw new NotImplementedException();
            }

            public Task<TestConsistencyMessage> FindByIdAsync(string messageId, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }

            public Task<string> GetMessageIdAsync(TestConsistencyMessage message, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }

            public Task<OperateResult> UpdateAsync(TestConsistencyMessage message, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }
        }
    }
}
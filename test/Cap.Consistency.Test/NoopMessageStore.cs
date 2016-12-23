using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cap.Consistency.Test
{
    public class NoopMessageStore : IConsistencyMessageStore<TestConsistencyMessage>
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
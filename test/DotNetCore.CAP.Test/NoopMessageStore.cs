using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Test
{
    public class NoopMessageStore : ICapMessageStore
    {
        public Task<OperateResult> CreateAsync(ConsistencyMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<OperateResult> DeleteAsync(ConsistencyMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<ConsistencyMessage> FindByIdAsync(string messageId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GeConsistencyMessageIdAsync(ConsistencyMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ConsistencyMessage> GetFirstEnqueuedMessageAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetMessageIdAsync(ConsistencyMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<OperateResult> UpdateAsync(ConsistencyMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
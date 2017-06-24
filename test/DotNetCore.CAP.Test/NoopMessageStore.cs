using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Test
{
    public class NoopMessageStore : ICapMessageStore
    {
        public Task<CapSentMessage> GetNextSentMessageToBeEnqueuedAsync()
        {
            throw new NotImplementedException();
        }

        public Task<OperateResult> RemoveSentMessageAsync(CapSentMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<OperateResult> StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<OperateResult> StoreSentMessageAsync(CapSentMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<OperateResult> UpdateReceivedMessageAsync(CapReceivedMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<OperateResult> UpdateSentMessageAsync(CapSentMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
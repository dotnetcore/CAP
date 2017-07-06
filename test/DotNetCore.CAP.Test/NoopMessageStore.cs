using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Test
{
    public class NoopMessageStore : ICapMessageStore
    {
        public Task<OperateResult> ChangeReceivedMessageStateAsync(CapReceivedMessage message, string statusName,
            bool autoSaveChanges = true)
        {
            throw new NotImplementedException();
        }

        public Task<OperateResult> ChangeSentMessageStateAsync(CapSentMessage message, string statusName,
            bool autoSaveChanges = true)
        {
            throw new NotImplementedException();
        }

        public Task<CapReceivedMessage> GetNextReceivedMessageToBeExcuted()
        {
            throw new NotImplementedException();
        }

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
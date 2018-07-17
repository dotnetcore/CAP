using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBStorageConnection : IStorageConnection
    {
        public bool ChangePublishedState(int messageId, string state)
        {
            throw new System.NotImplementedException();
        }

        public bool ChangeReceivedState(int messageId, string state)
        {
            throw new System.NotImplementedException();
        }

        public IStorageTransaction CreateTransaction()
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            throw new System.NotImplementedException();
        }

        public Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            throw new System.NotImplementedException();
        }

        public Task<int> StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}
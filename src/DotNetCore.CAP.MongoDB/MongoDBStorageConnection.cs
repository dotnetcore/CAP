using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBStorageConnection : IStorageConnection
    {
        private CapOptions _capOptions;
        private MongoDBOptions _options;
        private readonly IMongoClient _client;

        public MongoDBStorageConnection(CapOptions capOptions, MongoDBOptions options, IMongoClient client)
        {
            _capOptions = capOptions;
            _options = options;
            _client = client;
        }

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

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var collection = _client.GetDatabase(_options.Database).GetCollection<CapPublishedMessage>(_options.Published);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
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
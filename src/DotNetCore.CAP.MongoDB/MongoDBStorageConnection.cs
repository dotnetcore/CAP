using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBStorageConnection : IStorageConnection
    {
        private CapOptions _capOptions;
        private MongoDBOptions _options;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;

        public MongoDBStorageConnection(CapOptions capOptions, MongoDBOptions options, IMongoClient client)
        {
            _capOptions = capOptions;
            _options = options;
            _client = client;
            _database = _client.GetDatabase(_options.Database);
        }

        public bool ChangePublishedState(int messageId, string state)
        {
            var collection = _database.GetCollection<CapPublishedMessage>(_options.Published);

            var updateDef = Builders<CapPublishedMessage>
            .Update.Inc(x => x.Retries, 1)
            .Set(x => x.ExpiresAt, null)
            .Set(x => x.StatusName, state);

            var result =
            collection.UpdateOne(x => x.Id == messageId, updateDef);

            return result.ModifiedCount > 0;
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
            var collection = _database.GetCollection<CapPublishedMessage>(_options.Published);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<CapPublishedMessage>(_options.Published);
            return await
            collection.Find(x =>
            x.Retries < _capOptions.FailedRetryCount
            && x.Added < fourMinsAgo
            && (x.StatusName == StatusName.Failed || x.StatusName == StatusName.Scheduled))
            .Limit(200)
            .ToListAsync();
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var collection = _database.GetCollection<CapReceivedMessage>(_options.Received);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<CapReceivedMessage>(_options.Received);
            return await
            collection.Find(x =>
            x.Retries < _capOptions.FailedRetryCount
            && x.Added < fourMinsAgo
            && (x.StatusName == StatusName.Failed || x.StatusName == StatusName.Scheduled)
            ).Limit(200).ToListAsync();
        }

        public Task<int> StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}
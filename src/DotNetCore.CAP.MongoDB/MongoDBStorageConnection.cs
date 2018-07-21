using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using MongoDB.Bson;
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
            var collection = _database.GetCollection<CapPublishedMessage>(_options.PublishedCollection);

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
            var collection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            var updateDef = Builders<CapReceivedMessage>
            .Update.Inc(x => x.Retries, 1)
            .Set(x => x.ExpiresAt, null)
            .Set(x => x.StatusName, state);

            var result =
            collection.UpdateOne(x => x.Id == messageId, updateDef);

            return result.ModifiedCount > 0;
        }

        public IStorageTransaction CreateTransaction()
        {
            return new MongoDBStorageTransaction(_client, _options);
        }

        public void Dispose()
        {
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var collection = _database.GetCollection<CapPublishedMessage>(_options.PublishedCollection);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<CapPublishedMessage>(_options.PublishedCollection);
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
            var collection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            return await
            collection.Find(x =>
            x.Retries < _capOptions.FailedRetryCount
            && x.Added < fourMinsAgo
            && (x.StatusName == StatusName.Failed || x.StatusName == StatusName.Scheduled)
            ).Limit(200).ToListAsync();
        }

        public async Task<int> StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            var collection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            message.Id = await new MongoDBUtil().GetNextSequenceValueAsync(_database, _options.ReceivedCollection);

            collection.InsertOne(message);

            return message.Id;
        }
    }
}
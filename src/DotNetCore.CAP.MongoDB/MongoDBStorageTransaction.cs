using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using MongoDB.Driver;
using System;

namespace DotNetCore.CAP.MongoDB
{
    internal class MongoDBStorageTransaction : IStorageTransaction
    {
        private IMongoClient _client;
        private readonly MongoDBOptions _options;
        private readonly IMongoDatabase _database;
        private readonly IClientSessionHandle _session;

        public MongoDBStorageTransaction(IMongoClient client, MongoDBOptions options)
        {
            _client = client;
            _options = options;
            _database = client.GetDatabase(options.Database);
            _session = _client.StartSession();
            _session.StartTransaction();
        }

        public async Task CommitAsync()
        {
            await _session.CommitTransactionAsync();
        }

        public void Dispose()
        {
            _session.Dispose();
        }

        public void UpdateMessage(CapPublishedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var collection = _database.GetCollection<CapPublishedMessage>(_options.PublishedCollection);

            var updateDef = Builders<CapPublishedMessage>.Update
            .Set(x => x.Retries, message.Retries)
            .Set(x => x.Content, message.Content)
            .Set(x => x.ExpiresAt, message.ExpiresAt)
            .Set(x => x.StatusName, message.StatusName);

            collection.FindOneAndUpdate(_session, x => x.Id == message.Id, updateDef);
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var collection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            var updateDef = Builders<CapReceivedMessage>.Update
            .Set(x => x.Retries, message.Retries)
            .Set(x => x.Content, message.Content)
            .Set(x => x.ExpiresAt, message.ExpiresAt)
            .Set(x => x.StatusName, message.StatusName);

            collection.FindOneAndUpdate(_session, x => x.Id == message.Id, updateDef);
        }
    }
}
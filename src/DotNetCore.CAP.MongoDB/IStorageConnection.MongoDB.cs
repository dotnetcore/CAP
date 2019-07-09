// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly MongoDBOptions _options;

        public MongoDBStorageConnection(
            IOptions<CapOptions> capOptions,
            IOptions<MongoDBOptions> options, 
            IMongoClient client)
        {
            _capOptions = capOptions.Value;
            _options = options.Value;
            _client = client;
            _database = _client.GetDatabase(_options.DatabaseName);
        }

        public bool ChangePublishedState(long messageId, string state)
        {
            var collection = _database.GetCollection<PublishedMessage>(_options.PublishedCollection);

            var updateDef = Builders<PublishedMessage>
                .Update.Inc(x => x.Retries, 1)
                .Set(x => x.ExpiresAt, null)
                .Set(x => x.StatusName, state);

            var result =
                collection.UpdateOne(x => x.Id == messageId, updateDef);

            return result.ModifiedCount > 0;
        }

        public bool ChangeReceivedState(long messageId, string state)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);

            var updateDef = Builders<ReceivedMessage>
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

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(long id)
        {
            var collection = _database.GetCollection<PublishedMessage>(_options.PublishedCollection);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<PublishedMessage>(_options.PublishedCollection);
            return await collection
                .Find(x => x.Retries < _capOptions.FailedRetryCount
                           && x.Added < fourMinsAgo
                           && x.Version == _capOptions.Version
                           && (x.StatusName == StatusName.Failed || x.StatusName == StatusName.Scheduled))
                .Limit(200)
                .ToListAsync();
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(long id)
        {
            var collection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);

            return await collection
                .Find(x => x.Retries < _capOptions.FailedRetryCount
                           && x.Added < fourMinsAgo
                           && x.Version == _capOptions.Version
                           && (x.StatusName == StatusName.Failed || x.StatusName == StatusName.Scheduled))
                .Limit(200)
                .ToListAsync();
        }

        public void StoreReceivedMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            var collection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);

            var store = new ReceivedMessage()
            {
                Id = message.Id,
                Group = message.Group,
                Name = message.Name,
                Content = message.Content,
                Added = message.Added,
                StatusName = message.StatusName,
                ExpiresAt = message.ExpiresAt,
                Retries = message.Retries,
                Version = _capOptions.Version
            };

            collection.InsertOne(store);
        }
    }
}
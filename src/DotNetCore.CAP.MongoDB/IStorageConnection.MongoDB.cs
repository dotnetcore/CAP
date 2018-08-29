// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
        private readonly CapOptions _capOptions;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly MongoDBOptions _options;

        public MongoDBStorageConnection(CapOptions capOptions, MongoDBOptions options, IMongoClient client)
        {
            _capOptions = capOptions;
            _options = options;
            _client = client;
            _database = _client.GetDatabase(_options.DatabaseName);
        }

        public bool ChangePublishedState(long messageId, string state)
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

        public bool ChangeReceivedState(long messageId, string state)
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

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(long id)
        {
            var collection = _database.GetCollection<CapPublishedMessage>(_options.PublishedCollection);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<CapPublishedMessage>(_options.PublishedCollection);
            return await collection
                .Find(x => x.Retries < _capOptions.FailedRetryCount && x.Added < fourMinsAgo &&
                           (x.StatusName == StatusName.Failed || x.StatusName == StatusName.Scheduled))
                .Limit(200)
                .ToListAsync();
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(long id)
        {
            var collection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4);
            var collection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            return await collection
                .Find(x => x.Retries < _capOptions.FailedRetryCount && x.Added < fourMinsAgo &&
                           (x.StatusName == StatusName.Failed || x.StatusName == StatusName.Scheduled))
                .Limit(200)
                .ToListAsync();
        }

        public void StoreReceivedMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var collection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            collection.InsertOne(message);
        } 
    }
}
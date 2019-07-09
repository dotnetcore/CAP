// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    internal class MongoDBStorageTransaction : IStorageTransaction
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDBOptions _options;
        private readonly IClientSessionHandle _session;

        public MongoDBStorageTransaction(IMongoClient client, MongoDBOptions options)
        {
            _options = options;
            _database = client.GetDatabase(_options.DatabaseName);
            _session = client.StartSession();
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

            var collection = _database.GetCollection<PublishedMessage>(_options.PublishedCollection);

            var updateDef = Builders<PublishedMessage>.Update
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

            var collection = _database.GetCollection<ReceivedMessage>(_options.ReceivedCollection);

            var updateDef = Builders<ReceivedMessage>.Update
                .Set(x => x.Retries, message.Retries)
                .Set(x => x.Content, message.Content)
                .Set(x => x.ExpiresAt, message.ExpiresAt)
                .Set(x => x.StatusName, message.StatusName);

            collection.FindOneAndUpdate(_session, x => x.Id == message.Id, updateDef);
        }
    }
}
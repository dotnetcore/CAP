// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBCollectProcessor : ICollectProcessor
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger _logger;
        private readonly MongoDBOptions _options;
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        public MongoDBCollectProcessor(ILogger<MongoDBCollectProcessor> logger,
            MongoDBOptions options,
            IMongoClient client)
        {
            _options = options;
            _logger = logger;
            _database = client.GetDatabase(_options.DatabaseName);
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            _logger.LogDebug(
                $"Collecting expired data from collection [{_options.PublishedCollection}].");

            var publishedCollection = _database.GetCollection<CapPublishedMessage>(_options.PublishedCollection);
            var receivedCollection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            await publishedCollection.BulkWriteAsync(new[]
            {
                new DeleteManyModel<CapPublishedMessage>(
                    Builders<CapPublishedMessage>.Filter.Lt(x => x.ExpiresAt, DateTime.Now))
            });
            await receivedCollection.BulkWriteAsync(new[]
            {
                new DeleteManyModel<CapReceivedMessage>(
                    Builders<CapReceivedMessage>.Filter.Lt(x => x.ExpiresAt, DateTime.Now))
            });

            await context.WaitAsync(_waitingInterval);
        }
    }
}
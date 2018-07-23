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
        private readonly IMongoClient _client;
        private readonly MongoDBOptions _options;
        private readonly ILogger _logger;
        private readonly IMongoDatabase _database;
        private readonly TimeSpan _waitingInterval = TimeSpan.FromMinutes(5);

        public MongoDBCollectProcessor(IMongoClient client, MongoDBOptions options,
        ILogger<MongoDBCollectProcessor> logger)
        {
            _client = client;
            _options = options;
            _logger = logger;
            _database = client.GetDatabase(_options.Database);
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            _logger.LogDebug($"Collecting expired data from collection [{_options.Database}].[{_options.PublishedCollection}].");

            var publishedCollection = _database.GetCollection<CapPublishedMessage>(_options.PublishedCollection);
            var receivedCollection = _database.GetCollection<CapReceivedMessage>(_options.ReceivedCollection);

            await publishedCollection.BulkWriteAsync(new[]
            {
                new DeleteManyModel<CapPublishedMessage>(Builders<CapPublishedMessage>.Filter.Lt(x => x.ExpiresAt, DateTime.Now))
            });
            await receivedCollection.BulkWriteAsync(new[]
            {
                new DeleteManyModel<CapReceivedMessage>(Builders<CapReceivedMessage>.Filter.Lt(x => x.ExpiresAt, DateTime.Now))
            });

            await context.WaitAsync(_waitingInterval);
        }
    }
}
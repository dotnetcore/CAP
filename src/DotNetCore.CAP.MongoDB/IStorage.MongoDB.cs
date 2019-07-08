// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBStorage : IStorage
    {
        private readonly IOptions<CapOptions> _capOptions;
        private readonly IMongoClient _client;
        private readonly ILogger<MongoDBStorage> _logger;
        private readonly IOptions<MongoDBOptions> _options;

        public MongoDBStorage(
            IOptions<CapOptions> capOptions,
            IOptions<MongoDBOptions> options,
            IMongoClient client,
            ILogger<MongoDBStorage> logger)
        {
            _capOptions = capOptions;
            _options = options;
            _client = client;
            _logger = logger;
        }

        public IStorageConnection GetConnection()
        {
            return new MongoDBStorageConnection(_capOptions, _options, _client);
        }

        public IMonitoringApi GetMonitoringApi()
        {
            return new MongoDBMonitoringApi(_client, _options);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var options = _options.Value;
            var database = _client.GetDatabase(options.DatabaseName);
            var names = (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken)).ToList();

            if (names.All(n => n != options.ReceivedCollection))
            {
                await database.CreateCollectionAsync(options.ReceivedCollection, cancellationToken: cancellationToken);
            }

            if (names.All(n => n != options.PublishedCollection))
            {
                await database.CreateCollectionAsync(options.PublishedCollection,
                    cancellationToken: cancellationToken);
            }

            var receivedMessageIndexNames = new[] {
                nameof(ReceivedMessage.Name), nameof(ReceivedMessage.Added), nameof(ReceivedMessage.ExpiresAt),
                nameof(ReceivedMessage.StatusName), nameof(ReceivedMessage.Retries), nameof(ReceivedMessage.Version) };

            var publishedMessageIndexNames = new[] {
                nameof(PublishedMessage.Name), nameof(PublishedMessage.Added), nameof(PublishedMessage.ExpiresAt),
                nameof(PublishedMessage.StatusName), nameof(PublishedMessage.Retries), nameof(PublishedMessage.Version) };

            await Task.WhenAll(
                TryCreateIndexesAsync<ReceivedMessage>(options.ReceivedCollection, receivedMessageIndexNames),
                TryCreateIndexesAsync<PublishedMessage>(options.PublishedCollection, publishedMessageIndexNames)
                );

            _logger.LogDebug("Ensuring all create database tables script are applied.");

            async Task TryCreateIndexesAsync<T>(string collectionName, string[] indexNames)
            {
                var col = database.GetCollection<T>(collectionName);
                using (var cursor = await col.Indexes.ListAsync(cancellationToken))
                {
                    var existingIndexes = await cursor.ToListAsync(cancellationToken);
                    var existingIndexNames = existingIndexes.Select(o => o["name"].AsString).ToArray();
                    indexNames = indexNames.Except(existingIndexNames).ToArray();
                }

                if (indexNames.Any() == false)
                    return;

                var indexes = indexNames.Select(indexName =>
                {
                    var indexOptions = new CreateIndexOptions
                    {
                        Name = indexName,
                        Background = true,
                    };
                    var indexBuilder = Builders<T>.IndexKeys;
                    return new CreateIndexModel<T>(indexBuilder.Descending(indexName), indexOptions);
                }).ToArray();

                await col.Indexes.CreateManyAsync(indexes, cancellationToken);
            }
        }
    }
}
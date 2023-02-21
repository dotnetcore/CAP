// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB;

public class MongoDBStorageInitializer : IStorageInitializer
{
    private readonly IMongoClient _client;
    private readonly ILogger _logger;
    private readonly IOptions<MongoDBOptions> _options;
    private readonly IOptions<CapOptions> _capOptions;

    public MongoDBStorageInitializer(
        ILogger<MongoDBStorageInitializer> logger,
        IMongoClient client,
        IOptions<MongoDBOptions> options,IOptions<CapOptions> capOptions)
    {
        _capOptions = capOptions;
        _options = options;
        _logger = logger;
        _client = client;
    }

    public string GetPublishedTableName()
    {
        return _options.Value.PublishedCollection;
    }

    public string GetReceivedTableName()
    {
        return _options.Value.ReceivedCollection;
    }

    public string GetLockTableName()
    {
        return _options.Value.LockCollection;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var options = _options.Value;
        var database = _client.GetDatabase(options.DatabaseName);
        var names =
            (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            .ToList();

        if (names.All(n => n != options.ReceivedCollection))
            await database.CreateCollectionAsync(options.ReceivedCollection, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        if (names.All(n => n != options.PublishedCollection))
            await database.CreateCollectionAsync(options.PublishedCollection, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        if (_capOptions.Value.UseStorageLock&&names.All(n => n != options.LockCollection))
            await database.CreateCollectionAsync(options.LockCollection, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        
        await Task.WhenAll(
            TryCreateIndexesAsync<ReceivedMessage>(options.ReceivedCollection),
            TryCreateIndexesAsync<PublishedMessage>(options.PublishedCollection)).ConfigureAwait(false);
        
        if (_capOptions.Value.UseStorageLock)
        {
            await database.GetCollection<Lock>(options.LockCollection)
                .UpdateOneAsync(it=>it.Key==$"publish_retry_{_capOptions.Value.Version}",
                    Builders<Lock>.Update.Set(model=>model.Key,$"publish_retry_{_capOptions.Value.Version}").SetOnInsert(model=>model.LastLockTime,DateTime.MinValue), 
                    new UpdateOptions() { IsUpsert = true },cancellationToken);
            
            await database.GetCollection<Lock>(options.LockCollection)
                .UpdateOneAsync(it=>it.Key==$"received_retry_{_capOptions.Value.Version}",
                    Builders<Lock>.Update.Set(model=>model.Key,$"received_retry_{_capOptions.Value.Version}").SetOnInsert(model=>model.LastLockTime,DateTime.MinValue), 
                    new UpdateOptions() { IsUpsert = true },cancellationToken);
        }
       
        _logger.LogDebug("Ensuring all create database tables script are applied.");


        async Task TryCreateIndexesAsync<T>(string collectionName)
        {
            var indexNames = new[] { "Name", "Added", "ExpiresAt", "StatusName", "Retries", "Version" };
            var col = database.GetCollection<T>(collectionName);
            using (var cursor = await col.Indexes.ListAsync(cancellationToken).ConfigureAwait(false))
            {
                var existingIndexes = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
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
                    Background = true
                };
                var indexBuilder = Builders<T>.IndexKeys;
                return new CreateIndexModel<T>(indexBuilder.Descending(indexName), indexOptions);
            }).ToArray();

            await col.Indexes.CreateManyAsync(indexes, cancellationToken).ConfigureAwait(false);
        }
    }
}
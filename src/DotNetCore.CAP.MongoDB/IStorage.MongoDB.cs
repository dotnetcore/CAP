// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBStorage : IStorage
    {
        private readonly CapOptions _capOptions;
        private readonly IMongoClient _client;
        private readonly ILogger<MongoDBStorage> _logger;
        private readonly MongoDBOptions _options;

        public MongoDBStorage(CapOptions capOptions,
            MongoDBOptions options,
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

            var database = _client.GetDatabase(_options.DatabaseName);
            var names = (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken))?.ToList();

            if (names.All(n => n != _options.ReceivedCollection))
            {
                await database.CreateCollectionAsync(_options.ReceivedCollection, cancellationToken: cancellationToken);
            }

            if (names.All(n => n != _options.PublishedCollection))
            {
                await database.CreateCollectionAsync(_options.PublishedCollection, cancellationToken: cancellationToken);
            }

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }
    }
}
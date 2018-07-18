using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBStorage : IStorage
    {
        private readonly CapOptions _capOptions;
        private readonly MongoDBOptions _options;
        private readonly IMongoClient _client;
        private readonly ILogger<MongoDBStorage> _logger;

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

            var database = _client.GetDatabase(_options.Database);
            var names = (await database.ListCollectionNamesAsync())?.ToList();

            if (!names.Any(n => n == _options.Received))
            {
                await database.CreateCollectionAsync(_options.Received);
            }
            if (!names.Any(n => n == _options.Published))
            {
                await database.CreateCollectionAsync(_options.Published);
            }
            if (!names.Any(n => n == "Counter"))
            {
                await database.CreateCollectionAsync("Counter");
                var collection = database.GetCollection<BsonDocument>("Counter");
                await collection.InsertManyAsync(new BsonDocument[]
                {
                    new BsonDocument{{"_id", _options.Published}, {"sequence_value", 0}},
                    new BsonDocument{{"_id", _options.Received}, {"sequence_value", 0}}
                });
            }
        }
    }
}
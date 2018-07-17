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
            throw new System.NotImplementedException();
        }

        public IMonitoringApi GetMonitoringApi()
        {
            throw new System.NotImplementedException();
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var database = _client.GetDatabase(_options.Database);
            var names = (await database.ListCollectionNamesAsync())?.ToList();

            if (!names.Any(n => n == "Received"))
            {
                await database.CreateCollectionAsync("Received");
            }
            if (!names.Any(n => n == "Published"))
            {
                await database.CreateCollectionAsync("Published");
            }
        }
    }
}
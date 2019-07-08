using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB.Test
{
    public abstract class DatabaseTestHost : IDisposable
    {
        private string _connectionString;

        protected IServiceProvider Provider { get; private set; }
        protected IMongoClient MongoClient => Provider.GetService<IMongoClient>();
        protected IMongoDatabase Database => MongoClient.GetDatabase(MongoDBOptions.Value.DatabaseName);
        protected CapOptions CapOptions => Provider.GetService<IOptions<CapOptions>>().Value;
        protected IOptions<MongoDBOptions> MongoDBOptions => Provider.GetService<IOptions<MongoDBOptions>>();

        protected DatabaseTestHost()
        {
            CreateServiceCollection();
            CreateDatabase();
        }

        private void CreateDatabase()
        {
            Provider.GetService<MongoDBStorage>().InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        protected virtual void AddService(ServiceCollection serviceCollection)
        {

        }

        private void CreateServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();
            _connectionString = ConnectionUtil.ConnectionString;

            services.AddSingleton(new MongoDBOptions() { DatabaseConnection = _connectionString });
            services.ConfigureOptions<CapOptions>();
            services.AddSingleton<IMongoClient>(x => new MongoClient(_connectionString));
            services.AddSingleton<MongoDBStorage>();

            AddService(services);
            Provider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            MongoClient.DropDatabase(MongoDBOptions.Value.DatabaseName);
        }
    }
}
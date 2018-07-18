using System;
using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBCapOptionsExtension : ICapOptionsExtension
    {
        private Action<MongoDBOptions> _configure;

        public MongoDBCapOptionsExtension(Action<MongoDBOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapDatabaseStorageMarkerService>();
            services.AddSingleton<IStorage, MongoDBStorage>();
            services.AddSingleton<IStorageConnection, MongoDBStorageConnection>();
            services.AddScoped<ICapPublisher, CapPublisher>();
        }
    }
}
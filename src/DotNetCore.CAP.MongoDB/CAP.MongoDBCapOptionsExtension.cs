using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<MongoDBOptions> _configure;

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
            services.AddScoped<ICallbackPublisher, CapPublisher>();
            services.AddTransient<ICollectProcessor, MongoDBCollectProcessor>();

            var options = new MongoDBOptions();
            _configure?.Invoke(options);
            services.AddSingleton(options);
        }
    }
}
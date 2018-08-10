// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.MongoDB
{
    // ReSharper disable once InconsistentNaming
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
            services.AddTransient<IMongoTransaction, MongoTransaction>();

            var options = new MongoDBOptions();
            _configure?.Invoke(options);
            services.AddSingleton(options);
        }
    }
}
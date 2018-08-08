// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            services.TryAddSingleton<CapDatabaseStorageMarkerService>();
            services.TryAddSingleton<IStorage, MongoDBStorage>();
            services.TryAddSingleton<IStorageConnection, MongoDBStorageConnection>();
            services.TryAddScoped<ICapPublisher, CapPublisher>();
            services.TryAddScoped<ICallbackPublisher, CapPublisher>();
            services.TryAddTransient<ICollectProcessor, MongoDBCollectProcessor>();

            services.TryAddTransient<IMongoTransaction, MongoTransaction>();

            var options = new MongoDBOptions();
            _configure?.Invoke(options);
            services.TryAddSingleton(options);
        }
    }
}
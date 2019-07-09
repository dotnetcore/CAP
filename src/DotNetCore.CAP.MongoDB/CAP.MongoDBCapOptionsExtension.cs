// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

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
            services.AddSingleton<CapStorageMarkerService>();
            services.AddSingleton<IStorage, MongoDBStorage>();
            services.AddSingleton<IStorageConnection, MongoDBStorageConnection>();

            services.AddSingleton<ICapPublisher, MongoDBPublisher>();
            services.AddSingleton<ICallbackPublisher>(x => (MongoDBPublisher)x.GetService<ICapPublisher>());
            services.AddSingleton<ICollectProcessor, MongoDBCollectProcessor>();

            services.AddTransient<CapTransactionBase, MongoDBCapTransaction>();

            services.Configure(_configure);

            //Try to add IMongoClient if does not exists
            services.TryAddSingleton<IMongoClient>(x =>
            {
                var options = x.GetService<IOptions<MongoDBOptions>>().Value;
                return new MongoClient(options.DatabaseConnection);
            });
        }
    }
}
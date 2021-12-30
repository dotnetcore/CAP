// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Persistence;
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

            services.AddSingleton<IDataStorage, MongoDBDataStorage>();
            services.AddSingleton<IStorageInitializer, MongoDBStorageInitializer>(); 

            services.Configure(_configure);

            //Try to add IMongoClient if does not exists
            services.TryAddSingleton<IMongoClient>(x =>
            {
                var options = x.GetRequiredService<IOptions<MongoDBOptions>>().Value;
                return new MongoClient(options.DatabaseConnection);
            });
        }
    }
}
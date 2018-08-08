// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.PostgreSql;
using DotNetCore.CAP.Processor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class PostgreSqlCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<PostgreSqlOptions> _configure;

        public PostgreSqlCapOptionsExtension(Action<PostgreSqlOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<CapDatabaseStorageMarkerService>();
            services.TryAddSingleton<IStorage, PostgreSqlStorage>();
            services.TryAddSingleton<IStorageConnection, PostgreSqlStorageConnection>();
            services.TryAddScoped<ICapPublisher, CapPublisher>();
            services.TryAddScoped<ICallbackPublisher, CapPublisher>();
            services.TryAddTransient<ICollectProcessor, PostgreSqlCollectProcessor>();

            AddSingletonPostgreSqlOptions(services);
        }

        private void AddSingletonPostgreSqlOptions(IServiceCollection services)
        {
            var postgreSqlOptions = new PostgreSqlOptions();
            _configure(postgreSqlOptions);

            if (postgreSqlOptions.DbContextType != null)
            {
                services.TryAddSingleton(x =>
                {
                    using (var scope = x.CreateScope())
                    {
                        var provider = scope.ServiceProvider;
                        var dbContext = (DbContext)provider.GetService(postgreSqlOptions.DbContextType);
                        postgreSqlOptions.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
                        return postgreSqlOptions;
                    }
                });
            }
            else
            {
                services.TryAddSingleton(postgreSqlOptions);
            }
        }
    }
}
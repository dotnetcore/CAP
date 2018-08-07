// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class SqlServerCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<SqlServerOptions> _configure;

        public SqlServerCapOptionsExtension(Action<SqlServerOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<CapDatabaseStorageMarkerService>();
            services.TryAddSingleton<IStorage, SqlServerStorage>();
            services.TryAddSingleton<IStorageConnection, SqlServerStorageConnection>();
            services.TryAddScoped<ICapPublisher, CapPublisher>();
            services.TryAddScoped<ICallbackPublisher, CapPublisher>();
            services.TryAddTransient<ICollectProcessor, SqlServerCollectProcessor>();

            AddSqlServerOptions(services);
        }

        private void AddSqlServerOptions(IServiceCollection services)
        {
            var sqlServerOptions = new SqlServerOptions();

            _configure(sqlServerOptions);

            if (sqlServerOptions.DbContextType != null)
            {
                services.AddSingleton(x =>
                {
                    using (var scope = x.CreateScope())
                    {
                        var provider = scope.ServiceProvider;
                        var dbContext = (DbContext)provider.GetService(sqlServerOptions.DbContextType);
                        sqlServerOptions.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
                        return sqlServerOptions;
                    }
                });
            }
            else
            {
                services.AddSingleton(sqlServerOptions);
            }
        }
    }
}
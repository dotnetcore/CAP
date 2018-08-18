// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<CapDatabaseStorageMarkerService>();
            services.AddSingleton<IStorage, SqlServerStorage>();
            services.AddSingleton<IStorageConnection, SqlServerStorageConnection>();

            services.AddScoped<ICapPublisher, SqlServerPublisher>();
            services.AddScoped<ICallbackPublisher, SqlServerPublisher>();

            services.AddTransient<ICollectProcessor, SqlServerCollectProcessor>();
            services.AddTransient<CapTransactionBase, SqlServerCapTransaction>();

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
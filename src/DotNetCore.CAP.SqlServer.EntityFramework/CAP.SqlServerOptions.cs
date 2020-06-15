// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class ConfigureSqlServerOptions : IConfigureOptions<SqlServerOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ConfigureSqlServerOptions(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void Configure(SqlServerOptions options)
        {
            var type = options.GetType();
            var dbContextTypeProperty =
                type.GetProperty("DbContextType", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new ArgumentException("Can not get \"DbContextType\" property from SqlServerOptions");

            var dbContextTypeObj = dbContextTypeProperty.GetValue(options);
            if (dbContextTypeObj == null) return;
            if (!(dbContextTypeObj is Type dbContextType)) return;

            using var scope = _serviceScopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            using var dbContext = (DbContext)provider.GetRequiredService(dbContextType);
            options.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
        }
    }
}
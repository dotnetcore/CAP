// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class MySqlOptions : EFOptions
    {
        /// <summary>
        /// Gets or sets the database's connection string that will be used to store database entities.
        /// </summary>
        public string ConnectionString { get; set; } = default!;
    }

    internal class ConfigureMySqlOptions : IConfigureOptions<MySqlOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ConfigureMySqlOptions(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void Configure(MySqlOptions options)
        {
            if (options.DbContextType != null)
            {
                if (Helper.IsUsingType<ICapPublisher>(options.DbContextType))
                {
                    throw new InvalidOperationException("We detected that you are using ICapPublisher in DbContext, please change the configuration to use the storage extension directly to avoid circular references! eg:  x.UseMySql()");
                }

                using var scope = _serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider;
                using var dbContext = (DbContext)provider.GetRequiredService(options.DbContextType);
                options.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
            }
        }
    }
}
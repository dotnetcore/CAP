// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class OracleOptions : EFOptions
    {
        /// <summary>
        /// Gets or sets the database's connection string that will be used to store database entities.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets the username of connection string that will be use to create table for owner.
        /// </summary>
        /// <returns></returns>
        public string GetUserName()
        {
            var ms = new Regex(@"user\sid=\w+;", RegexOptions.IgnoreCase)
                 .Match(ConnectionString);
            if (string.IsNullOrEmpty(ms.Value))
                throw new System.InvalidOperationException("connection string must has 'User Id=your userid;'");
            return ms.Value.Split('=')[1].TrimEnd(';').Trim();
        }
    }

    internal class ConfigureOracleOptions : IConfigureOptions<OracleOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ConfigureOracleOptions(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void Configure(OracleOptions options)
        {
            if (options.DbContextType != null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider;
                using var dbContext = (DbContext)provider.GetRequiredService(options.DbContextType);
                options.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
            }
        }
    }
}
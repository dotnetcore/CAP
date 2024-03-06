﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP;

public class PostgreSqlOptions : EFOptions
{
    /// <summary>
    /// Gets or sets the database's connection string that will be used to store database entities.
    /// </summary>
    [Obsolete("Use .DataSource = NpgsqlDataSource.Create(<connectionString>) for same behavior.")]
    public string ConnectionString { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the Npgsql data source that will be used to store database entities.
    /// </summary>
    public NpgsqlDataSource? DataSource { get; set; }

    /// <summary>
    /// Creates an Npgsql connection from the configured data source.
    /// </summary>
    internal NpgsqlConnection CreateConnection()
    {
        return DataSource != null ? DataSource.CreateConnection() : new NpgsqlConnection(ConnectionString);
    }
}

internal class ConfigurePostgreSqlOptions : IConfigureOptions<PostgreSqlOptions>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ConfigurePostgreSqlOptions(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Configure(PostgreSqlOptions options)
    {
        if (options.DbContextType == null) return;

        if (Helper.IsUsingType<ICapPublisher>(options.DbContextType))
            throw new InvalidOperationException(
                "We detected that you are using ICapPublisher in DbContext, please change the configuration to use the storage extension directly to avoid circular references! eg:  x.UsePostgreSql()");

        using var scope = _serviceScopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        using var dbContext = (DbContext)provider.GetRequiredService(options.DbContextType);
        var connectionString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(connectionString);
        options.ConnectionString = connectionString;
    }
}
using System;
using HuaweiCloud.GaussDB;

namespace DotNetCore.CAP.GaussDB.Test;

internal static class ConnectionUtil
{
    private const string ConnectionStringEnvironmentVariable = "Cap_GaussDB_ConnectionString";
    private const string ConnectionStringTemplateEnvironmentVariable = "Cap_GaussDB_ConnectionStringTemplate";

    public const string DefaultDatabase = "DB_PG";

    public static bool IsConnectionAvailable =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable))
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringTemplateEnvironmentVariable));

    public static string GetConnectionString(string databaseName = DefaultDatabase)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
            ?? Environment.GetEnvironmentVariable(ConnectionStringTemplateEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} or {ConnectionStringTemplateEnvironmentVariable} before running GaussDB integration tests.");

        var builder = new GaussDBConnectionStringBuilder(connectionString);
        builder.NoResetOnClose = true;
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            builder.Database = databaseName;
        }

        return builder.ToString();
    }

    public static GaussDBConnection CreateConnection(string databaseName = DefaultDatabase)
    {
        return new GaussDBConnection(GetConnectionString(databaseName));
    }
}

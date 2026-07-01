using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.GaussDB;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Serialization;
using HuaweiCloud.GaussDB;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.GaussDB.Test;

internal static class GaussDBTestSupport
{
    public const string Version = "v1";

    public static async Task<(GaussDBDataStorage Storage, GaussDBStorageInitializer Initializer)> CreateStorageAsync(
        string databaseName = ConnectionUtil.DefaultDatabase,
        bool useStorageLock = true)
    {
        var capOptions = Options.Create(new CapOptions { UseStorageLock = useStorageLock });
        var configuredOptions = new GaussDBOptions { ConnectionString = ConnectionUtil.GetConnectionString(databaseName) };
        typeof(EFOptions).GetProperty("Version", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(configuredOptions, Version);
        var databaseOptions = Options.Create(configuredOptions);
        var initializer = new GaussDBStorageInitializer(
            NullLogger<GaussDBStorageInitializer>.Instance, databaseOptions, capOptions);
        await initializer.InitializeAsync(CancellationToken.None);
        var storage = new GaussDBDataStorage(
            databaseOptions, capOptions, initializer, new JsonUtf8Serializer(capOptions), new SnowflakeId(1));
        return (storage, initializer);
    }

    public static Message CreateMessage(string id)
    {
        return new Message(new Dictionary<string, string> { [Headers.MessageId] = id }, null);
    }

    public static string NextId()
    {
        return new SnowflakeId(1).NextId().ToString();
    }

    public static async Task<string> GetStatusAsync(string table, string id, string databaseName = ConnectionUtil.DefaultDatabase)
    {
        await using var connection = ConnectionUtil.CreateConnection(databaseName);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT \"StatusName\" FROM {table} WHERE \"Id\" = @Id";
        command.Parameters.Add(new GaussDBParameter("@Id", long.Parse(id)));
        return await command.ExecuteScalarAsync() as string;
    }

    public static async Task<int> CountByNameAsync(string table, string name, string databaseName = ConnectionUtil.DefaultDatabase)
    {
        await using var connection = ConnectionUtil.CreateConnection(databaseName);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {table} WHERE \"Name\" = @Name";
        command.Parameters.Add(new GaussDBParameter("@Name", name));
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public static async Task DeleteByIdAsync(string table, string id, string databaseName = ConnectionUtil.DefaultDatabase)
    {
        await using var connection = ConnectionUtil.CreateConnection(databaseName);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {table} WHERE \"Id\" = @Id";
        command.Parameters.Add(new GaussDBParameter("@Id", long.Parse(id)));
        await command.ExecuteNonQueryAsync();
    }

    public static async Task DeleteByNameAsync(string table, string name, string databaseName = ConnectionUtil.DefaultDatabase)
    {
        await using var connection = ConnectionUtil.CreateConnection(databaseName);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {table} WHERE \"Name\" = @Name";
        command.Parameters.Add(new GaussDBParameter("@Name", name));
        await command.ExecuteNonQueryAsync();
    }
}

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using HuaweiCloud.GaussDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

[Collection(GaussDBCollection.Name)]
public class GaussDBTransactionTests
{
    [Fact]
    public async Task AdoTransaction_CommitPersistsInsertedRecord()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        await EnsureSchemaAsync();
        var id = DateTime.UtcNow.Ticks;
        using var provider = CreateCapProvider();
        var publisher = new TransactionTestPublisher(provider);
        await using var connection = ConnectionUtil.CreateConnection();

        var transaction = connection.BeginTransaction(publisher);
        await InsertPublishedAsync(connection, (DbTransaction)transaction.DbTransaction!, id);
        transaction.Commit();

        Assert.Equal(1, await CountPublishedAsync(id));
        await DeletePublishedAsync(id);
    }

    [Fact]
    public async Task AdoTransaction_RollbackRemovesInsertedRecord()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        await EnsureSchemaAsync();
        var id = DateTime.UtcNow.Ticks;
        using var provider = CreateCapProvider();
        var publisher = new TransactionTestPublisher(provider);
        await using var connection = ConnectionUtil.CreateConnection();

        var transaction = connection.BeginTransaction(publisher);
        await InsertPublishedAsync(connection, (DbTransaction)transaction.DbTransaction!, id);
        transaction.Rollback();

        Assert.Equal(0, await CountPublishedAsync(id));
    }

    [Fact]
    public async Task AdoTransactionAsync_RollbackRemovesInsertedRecord()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        await EnsureSchemaAsync();
        var id = DateTime.UtcNow.Ticks;
        using var provider = CreateCapProvider();
        var publisher = new TransactionTestPublisher(provider);
        await using var connection = ConnectionUtil.CreateConnection();

        var transaction = await connection.BeginTransactionAsync(publisher, cancellationToken: CancellationToken.None);
        await InsertPublishedAsync(connection, (DbTransaction)transaction.DbTransaction!, id);
        await transaction.RollbackAsync(CancellationToken.None);

        Assert.Equal(0, await CountPublishedAsync(id));
    }

    [Fact]
    public async Task EntityFrameworkTransaction_RollbackRemovesInsertedRecord()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        await EnsureSchemaAsync();
        var id = DateTime.UtcNow.Ticks;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DotNetCore.CAP.Transport.IDispatcher, TransactionTestDispatcher>();
        services.AddDbContext<TestDbContext>(options => options.UseGaussDB(ConnectionUtil.GetConnectionString()));
        using var provider = services.BuildServiceProvider();
        var publisher = new TransactionTestPublisher(provider);
        await using var context = provider.GetRequiredService<TestDbContext>();

        var transaction = context.Database.BeginTransaction(publisher);
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "cap"."published" ("Id","Version","Name","Retries","Added","StatusName")
            VALUES ({0}, 'v1', 'ef.transaction', 0, CURRENT_TIMESTAMP, 'Queued')
            """, id);
        transaction.Rollback();

        Assert.Equal(0, await CountPublishedAsync(id));
    }

    [Fact]
    public async Task EntityFrameworkTransactionAsync_CommitPersistsInsertedRecord()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        await EnsureSchemaAsync();
        var id = DateTime.UtcNow.Ticks;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DotNetCore.CAP.Transport.IDispatcher, TransactionTestDispatcher>();
        services.AddDbContext<TestDbContext>(options => options.UseGaussDB(ConnectionUtil.GetConnectionString()));
        using var provider = services.BuildServiceProvider();
        var publisher = new TransactionTestPublisher(provider);
        await using var context = provider.GetRequiredService<TestDbContext>();

        var transaction = await context.Database.BeginTransactionAsync(publisher, cancellationToken: CancellationToken.None);
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "cap"."published" ("Id","Version","Name","Retries","Added","StatusName")
            VALUES ({0}, 'v1', 'ef.transaction.async', 0, CURRENT_TIMESTAMP, 'Queued')
            """, id);
        await transaction.CommitAsync(CancellationToken.None);

        Assert.Equal(1, await CountPublishedAsync(id));
        await DeletePublishedAsync(id);
    }

    private static ServiceProvider CreateCapProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DotNetCore.CAP.Transport.IDispatcher, TransactionTestDispatcher>();
        return services.BuildServiceProvider();
    }

    private static async Task EnsureSchemaAsync()
    {
        var options = Options.Create(new GaussDBOptions { ConnectionString = ConnectionUtil.GetConnectionString() });
        var initializer = new GaussDBStorageInitializer(
            NullLogger<GaussDBStorageInitializer>.Instance, options,
            Options.Create(new CapOptions { UseStorageLock = true }));
        await initializer.InitializeAsync(CancellationToken.None);
    }

    private static async Task InsertPublishedAsync(GaussDBConnection connection, DbTransaction transaction, long id)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (GaussDBTransaction)transaction;
        command.CommandText = """
            INSERT INTO "cap"."published" ("Id","Version","Name","Retries","Added","StatusName")
            VALUES (@Id, 'v1', 'ado.transaction', 0, CURRENT_TIMESTAMP, 'Queued')
            """;
        command.Parameters.Add(new GaussDBParameter("@Id", id));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountPublishedAsync(long id)
    {
        await using var connection = ConnectionUtil.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM \"cap\".\"published\" WHERE \"Id\" = @Id";
        command.Parameters.Add(new GaussDBParameter("@Id", id));
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task DeletePublishedAsync(long id)
    {
        await using var connection = ConnectionUtil.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM \"cap\".\"published\" WHERE \"Id\" = @Id";
        command.Parameters.Add(new GaussDBParameter("@Id", id));
        await command.ExecuteNonQueryAsync();
    }
}

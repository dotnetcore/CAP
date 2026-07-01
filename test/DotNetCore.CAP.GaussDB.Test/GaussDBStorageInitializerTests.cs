using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

[Collection(GaussDBCollection.Name)]
public class GaussDBStorageInitializerTests
{
    [Theory]
    [InlineData("DB_A", 0)]
    [InlineData("DB_B", 1)]
    [InlineData("DB_C", 2)]
    [InlineData("DB_PG", 3)]
    public async Task InitializeAsync_CreatesCapObjectsAndDetectsCompatibilityMode(string databaseName, int expectedMode)
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        var initializer = new GaussDBStorageInitializer(
            NullLogger<GaussDBStorageInitializer>.Instance,
            Options.Create(new GaussDBOptions { ConnectionString = ConnectionUtil.GetConnectionString(databaseName) }),
            Options.Create(new CapOptions { UseStorageLock = true }));

        await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal(expectedMode, initializer.DBCompatibilityMode);
    }

    [Fact]
    public async Task InitializeAsync_ThrowsWhenDatabaseDoesNotExist()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        var initializer = new GaussDBStorageInitializer(
            NullLogger<GaussDBStorageInitializer>.Instance,
            Options.Create(new GaussDBOptions
            {
                ConnectionString = ConnectionUtil.GetConnectionString("DB_DOES_NOT_EXIST"),
                StartupCheckDatabaseExistsMaxRetries = 0,
                StartupCheckDatabaseExistsBaseDelay = TimeSpan.Zero,
                StartupCheckDatabaseExistsMaxDelay = TimeSpan.Zero
            }),
            Options.Create(new CapOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.InitializeAsync(CancellationToken.None));
    }
}

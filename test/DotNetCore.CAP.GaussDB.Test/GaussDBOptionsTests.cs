using System.Reflection;
using HuaweiCloud.GaussDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

public class GaussDBOptionsTests
{
    [Fact]
    public void UseGaussDB_RegistersGaussDBStorageMarker()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCap(options => options.UseGaussDB("Host=unused"));

        using var provider = services.BuildServiceProvider();

        var marker = provider.GetRequiredService<CapStorageMarkerService>();
        Assert.Equal("GaussDB", marker.Name);
    }

    [Fact]
    public void CreateConnection_UsesConfiguredConnectionString()
    {
        const string connectionString = "Host=unused;Database=cap";
        var options = new GaussDBOptions
        {
            ConnectionString = connectionString,
            EnableAutoSetNoResetOnClose = false
        };

        using var connection = CreateConnection(options);

        Assert.Equal(connectionString, connection.ConnectionString);
    }

    [Fact]
    public void CreateConnection_DefaultsNoResetOnCloseWhenNotConfigured()
    {
        var options = new GaussDBOptions { ConnectionString = "Host=unused;Database=cap" };

        using var connection = CreateConnection(options);
        var builder = new GaussDBConnectionStringBuilder(connection.ConnectionString);

        Assert.True(builder.NoResetOnClose);
    }

    [Fact]
    public void CreateConnection_PreservesConfiguredNoResetOnClose()
    {
        var options = new GaussDBOptions
        {
            ConnectionString = "Host=unused;Database=cap;No Reset On Close=false"
        };

        using var connection = CreateConnection(options);
        var builder = new GaussDBConnectionStringBuilder(connection.ConnectionString);

        Assert.False(builder.NoResetOnClose);
    }

    [Fact]
    public void CreateConnection_UsesConfiguredDataSource()
    {
        using var dataSource = GaussDBDataSource.Create("Host=unused;Database=cap");
        var options = new GaussDBOptions { DataSource = dataSource };

        using var connection = CreateConnection(options);

        var property = typeof(GaussDBConnection).GetProperty(
            "GaussDBDataSource", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        Assert.Same(dataSource, property.GetValue(connection));
    }

    [Fact]
    public void UseGaussDB_ActionAppliesCustomOptionsAndCapVersion()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCap(options =>
        {
            options.Version = "custom-version";
            options.UseGaussDB(gaussdb =>
            {
                gaussdb.ConnectionString = "Host=unused;Database=cap";
                gaussdb.Schema = "custom_schema";
                gaussdb.AdminDatabaseName = "postgres";
            });
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GaussDBOptions>>().Value;

        Assert.Equal("Host=unused;Database=cap", options.ConnectionString);
        Assert.Equal("custom_schema", options.Schema);
        Assert.Equal("postgres", options.AdminDatabaseName);
        Assert.Equal("custom-version", GetVersion(options));
    }

    [Fact]
    public void UseEntityFramework_ReadsConfiguredGaussDBConnectionString()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options => options.UseGaussDB("Host=unused;Database=cap"));
        services.AddCap(options => options.UseEntityFramework<TestDbContext>(ef => ef.Schema = "ef_schema"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GaussDBOptions>>().Value;

        Assert.Equal("Host=unused;Database=cap", options.ConnectionString);
        Assert.Null(options.DataSource);
        Assert.Equal("ef_schema", options.Schema);
    }

    private static GaussDBConnection CreateConnection(GaussDBOptions options)
    {
        var method = typeof(GaussDBOptions).GetMethod(
            "CreateConnection", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<GaussDBConnection>(method.Invoke(options, null));
    }

    private static string GetVersion(GaussDBOptions options)
    {
        var property = typeof(EFOptions).GetProperty("Version", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<string>(property.GetValue(options));
    }
}

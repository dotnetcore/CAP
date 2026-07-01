using DotNetCore.CAP;
using HuaweiCloud.GaussDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

public class GaussDBEntityFrameworkOptionsTests
{
    [Fact]
    public void UseEntityFramework_ReadsConfiguredGaussDBDataSource()
    {
        using var dataSource = GaussDBDataSource.Create("Host=unused;Database=cap");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options => options.UseGaussDB(dataSource));
        services.AddCap(options => options.UseEntityFramework<TestDbContext>());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GaussDBOptions>>().Value;

        Assert.Same(dataSource, options.DataSource);
        Assert.Null(options.ConnectionString);
    }
}

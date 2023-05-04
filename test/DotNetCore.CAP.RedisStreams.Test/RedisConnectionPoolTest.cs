using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DotNetCore.CAP.RedisStreams;

public class RedisConnectionPoolTest
{
    [Fact]
    public async Task ConnectAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging().AddCap(options => options.UseRedis(options =>
        {
            options.Configuration = ConfigurationOptions.Parse("localhost,abortConnect=false,connectTimeout=500");
            options.ConnectionPoolSize = (uint)Random.Shared.Next(2, 5);
        }));

        await using var buildServiceProvider = services.BuildServiceProvider();
        await using var scope = buildServiceProvider.CreateAsyncScope();

        var connectionPool = scope.ServiceProvider.GetRequiredService<IRedisConnectionPool>();

        for (long index = scope.ServiceProvider.GetRequiredService<IOptions<CapRedisOptions>>().Value.ConnectionPoolSize + 1; index >= 0; index--)
        {
            Assert.NotNull(await connectionPool.ConnectAsync());
        }
    }
}
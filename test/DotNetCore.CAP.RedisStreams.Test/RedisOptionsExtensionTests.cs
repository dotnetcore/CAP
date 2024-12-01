using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.RedisStreams.Test;

public class RedisOptionsExtensionTests
{
    [Fact]
    public void AddServices_Should_Register_All_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        var configureOptions = new Action<CapRedisOptions>(options => { });

        var extension = new RedisOptionsExtension(configureOptions);

        // Act
        extension.AddServices(services);
        services.AddLogging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<CapMessageQueueMakerService>());
        Assert.NotNull(serviceProvider.GetService<IRedisStreamManager>());
        Assert.NotNull(serviceProvider.GetService<IConsumerClientFactory>());
        Assert.NotNull(serviceProvider.GetService<ITransport>());
        Assert.NotNull(serviceProvider.GetService<IRedisConnectionPool>());

        // Verify the post-configuration for CapRedisOptions
        var postConfig = serviceProvider.GetServices<IPostConfigureOptions<CapRedisOptions>>();
        Assert.Single(postConfig);
    }

    [Fact]
    public void AddServices_Should_Configure_CapRedisOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configureOptions = new Action<CapRedisOptions>(options =>
        {
            options.StreamEntriesCount = 20;
            options.ConnectionPoolSize = 30;
        });

        var extension = new RedisOptionsExtension(configureOptions);

        // Act
        extension.AddServices(services);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<CapRedisOptions>>()?.Value;

        // Assert
        Assert.NotNull(options);
        Assert.Equal((uint)20, options!.StreamEntriesCount);
        Assert.Equal((uint)30, options.ConnectionPoolSize);
    }
}

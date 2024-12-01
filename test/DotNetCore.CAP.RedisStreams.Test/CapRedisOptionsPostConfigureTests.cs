using StackExchange.Redis;
using System.Net;

namespace DotNetCore.CAP.RedisStreams.Test;

public class CapRedisOptionsPostConfigureTests
{
    [Fact]
    public void PostConfigure_Should_Set_Default_Values()
    {
        // Arrange
        var options = new CapRedisOptions();
        var postConfigure = new CapRedisOptionsPostConfigure();

        // Act
        postConfigure.PostConfigure(null, options);

        // Assert
        Assert.NotNull(options.Configuration);
        Assert.Equal((uint)10, options.StreamEntriesCount);
        Assert.Equal((uint)10, options.ConnectionPoolSize);
        Assert.Single(options.Configuration.EndPoints);
        Assert.Equal($"{IPAddress.Loopback}:6379", options.Configuration.EndPoints.First().ToString());
    }

    [Fact]
    public void PostConfigure_Should_Not_Override_Existing_Values()
    {
        // Arrange
        var options = new CapRedisOptions
        {
            StreamEntriesCount = 50,
            ConnectionPoolSize = 100,
            Configuration = new ConfigurationOptions
            {
                EndPoints = { "localhost:6379" }
            }
        };

        var postConfigure = new CapRedisOptionsPostConfigure();

        // Act
        postConfigure.PostConfigure(null, options);

        // Assert
        Assert.NotNull(options.Configuration);
        Assert.Equal((uint)50, options.StreamEntriesCount);
        Assert.Equal((uint)100, options.ConnectionPoolSize);
        Assert.Single(options.Configuration.EndPoints);
        Assert.Contains("localhost:6379", options.Configuration.ToString());
    }
}

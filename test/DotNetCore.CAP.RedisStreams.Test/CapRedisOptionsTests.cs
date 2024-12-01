using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams.Test;
public class CapRedisOptionsTests
{
    [Fact]
    public void Default_Endpoint_Should_Be_Empty()
    {
        // Arrange
        var options = new CapRedisOptions();

        // Act
        var endpoint = options.Endpoint;

        // Assert
        Assert.Equal(string.Empty, endpoint);
    }

    [Fact]
    public void Endpoint_Should_Return_Correct_Value_When_Configuration_Is_Set()
    {
        // Arrange
        var options = new CapRedisOptions
        {
            Configuration = new ConfigurationOptions
            {
                EndPoints = { "localhost:6379" }
            }
        };

        // Act
        var endpoint = options.Endpoint;

        // Assert
        Assert.Contains("localhost:6379", endpoint);
    }

    [Fact]
    public void StreamEntriesCount_Should_Be_Set_Correctly()
    {
        // Arrange
        CapRedisOptions options = new ()
        {
            // Act
            StreamEntriesCount = 10
        };

        // Assert
        Assert.Equal((uint)10, options.StreamEntriesCount);
    }

    [Fact]
    public void ConnectionPoolSize_Should_Be_Set_Correctly()
    {
        // Arrange
        var options = new CapRedisOptions
        {
            // Act
            ConnectionPoolSize = 5
        };

        // Assert
        Assert.Equal((uint)5, options.ConnectionPoolSize);
    }

    [Fact]
    public void OnConsumeError_Should_Invoke_Correctly()
    {
        // Arrange
        var options = new CapRedisOptions();
        var invoked = false;

        options.OnConsumeError = context =>
        {
            invoked = true;
            Assert.NotNull(context.Exception);
            Assert.Equal("Test exception", context.Exception.Message);
            Assert.Null(context.Entry);
            return Task.CompletedTask;
        };

        var errorContext = new CapRedisOptions.ConsumeErrorContext(new Exception("Test exception"), null);

        // Act
        options.OnConsumeError?.Invoke(errorContext);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public void ConsumeErrorContext_Should_Hold_Correct_Values()
    {
        // Arrange
        var exception = new Exception("Error message");
        var streamEntry = new StreamEntry("entryId", null);
        var context = new CapRedisOptions.ConsumeErrorContext(exception, streamEntry);

        // Assert
        Assert.Equal(exception, context.Exception);
        Assert.Equal(streamEntry, context.Entry);
    }
}

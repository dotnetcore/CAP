using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace DotNetCore.CAP.RedisStreams.Test;

public class RedisConnectionPoolTests : IAsyncLifetime
{
    private readonly Mock<IOptions<CapRedisOptions>> _optionsMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<AsyncLazyRedisConnection>> _loggerMock;
    private readonly CapRedisOptions _redisOptions;
    private RedisContainer? _redisContainer;

    public RedisConnectionPoolTests()
    {
        _redisOptions = new CapRedisOptions
        {
            ConnectionPoolSize = 5,
            Configuration = ConfigurationOptions.Parse("localhost:6379")
        };

        _optionsMock = new Mock<IOptions<CapRedisOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_redisOptions);

        // Mock ILoggerFactory and ILogger for AsyncLazyRedisConnection
        _loggerMock = new Mock<ILogger<AsyncLazyRedisConnection>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
    }

    [Fact]
    public void Init_Should_Create_Correct_Number_Of_Connections()
    {
        // Arrange
        var pool = new RedisConnectionPool(_optionsMock.Object, _loggerFactoryMock.Object);

        // Act
        var connectionsField = typeof(RedisConnectionPool)
            .GetField("_connections", BindingFlags.NonPublic | BindingFlags.Instance);

        var connections = (ConcurrentBag<AsyncLazyRedisConnection>)connectionsField!.GetValue(pool)!;

        // Assert
        Assert.NotNull(connections);
        Assert.Equal(_redisOptions.ConnectionPoolSize, (uint)connections.Count);
    }

    [Fact]
    public async Task ConnectAsync_Should_Return_Available_Connection()
    {
        // Arrange
        var pool = new RedisConnectionPool(_optionsMock.Object, _loggerFactoryMock.Object);

        // Act
        var connection = await pool.ConnectAsync();

        // Assert
        Assert.NotNull(connection);
        Assert.IsAssignableFrom<IConnectionMultiplexer>(connection);
    }

    [Fact]
    public void Dispose_Should_Cleanup_Connections()
    {
        // Arrange
        var pool = new RedisConnectionPool(_optionsMock.Object, _loggerFactoryMock.Object);

        // Act
        pool.Dispose();

        var isDisposedField = typeof(RedisConnectionPool)
            .GetField("_isDisposed", BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var isDisposed = (bool)isDisposedField!.GetValue(pool)!;

        // Assert
        Assert.True(isDisposed);
    }

    [Fact]
    public void Dispose_Should_Not_Throw_If_Already_Disposed()
    {
        // Arrange
        var pool = new RedisConnectionPool(_optionsMock.Object, _loggerFactoryMock.Object);

        // Act & Assert
        pool.Dispose();
        var exception = Record.Exception(() => pool.Dispose());
        Assert.Null(exception);
    }

    public Task InitializeAsync()
    {
        // Create a Redis container using TestContainers
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7.0")
            .WithPortBinding(6379, 6379)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();

        return _redisContainer.StartAsync();
    }

    public Task DisposeAsync()
    {
        var task = _redisContainer?.StopAsync();
        return task ?? Task.CompletedTask;
    }
}

// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams;

internal class RedisEvents
{
    private readonly IConnectionMultiplexer _connection;
    private readonly ILogger _logger;

    public RedisEvents(IConnectionMultiplexer connection, ILogger logger)
    {
        _logger = logger;
        _connection = connection;
        _connection.ErrorMessage += Connection_ErrorMessage;
        _connection.ConnectionRestored += Connection_ConnectionRestored;
        _connection.ConnectionFailed += Connection_ConnectionFailed;
    }

    private void Connection_ConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "Connection failed!, {message}, for endpoint:{endPoint}, failure type:{failureType}, connection type:{connectionType}", e.Exception?.Message, e.EndPoint, e.FailureType, e.ConnectionType);
    }

    private void Connection_ConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        _logger.LogWarning("Connection restored back!, {message}, for endpoint:{endPoint}, failure type:{failureType}, connection type:{connectionType}", e.Exception?.Message, e.EndPoint, e.FailureType, e.ConnectionType);
    }

    private void Connection_ErrorMessage(object? sender, RedisErrorEventArgs e)
    {
        if (e.Message.GetRedisErrorType() == RedisErrorTypes.Unknown)
        {
            _logger.LogError("Server replied with error, {message}, for endpoint:{endPoint}", e.Message, e.EndPoint);
        }
    }
}

internal static class RedisConnectionExtensions
{
    public static void LogEvents(this IConnectionMultiplexer connection, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(connection);

        ArgumentNullException.ThrowIfNull(logger);

        _ = new RedisEvents(connection, logger);
    }
}
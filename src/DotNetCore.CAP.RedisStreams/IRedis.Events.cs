// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams
{
    internal class RedisEvents
    {
        private readonly ILogger _logger;

        public RedisEvents(IConnectionMultiplexer connection, ILogger logger)
        {
            this._logger = logger;
            connection.ErrorMessage += Connection_ErrorMessage;
            connection.ConnectionRestored += Connection_ConnectionRestored;
            connection.ConnectionFailed += Connection_ConnectionFailed;
        }

        private void Connection_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            _logger.LogError(e.Exception,
                $"Connection failed!, {e.Exception?.Message}, for endpoint:{e.EndPoint}, failure type:{e.FailureType}, connection type:{e.ConnectionType}");
        }

        private void Connection_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            _logger.LogWarning(
                $"Connection restored back!, {e.Exception?.Message}, for endpoint:{e.EndPoint}, failure type:{e.FailureType}, connection type:{e.ConnectionType}");
        }

        private void Connection_ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            if (e.Message.GetRedisErrorType() == RedisErrorTypes.Unknown)
            {
                _logger.LogError($"Server replied with error, {e.Message}, for endpoint:{e.EndPoint}");
            }
        }
    }

    internal static class RedisConnectionExtensions
    {
        public static void LogEvents(this IConnectionMultiplexer connection, ILogger logger)
        {
            if (connection is null) throw new ArgumentNullException(nameof(connection));

            if (logger is null) throw new ArgumentNullException(nameof(logger));

            _ = new RedisEvents(connection, logger);
        }
    }
}
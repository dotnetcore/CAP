using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{    
    class RedisEvents
    {
        private readonly ILogger logger;

        public RedisEvents(IConnectionMultiplexer connection, ILogger logger)
        {
            this.logger = logger;
            connection.ErrorMessage += Connection_ErrorMessage;
            connection.ConnectionRestored += Connection_ConnectionRestored;
            connection.ConnectionFailed += Connection_ConnectionFailed;
        }

        private void Connection_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            logger.LogError(e.Exception, $"Connection failed!, {e.Exception?.Message}, for endpoint:{e.EndPoint}, failure type:{e.FailureType}, connection type:{e.ConnectionType}");
        }

        private void Connection_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            logger.LogWarning($"Connection restored back!, {e.Exception?.Message}, for endpoint:{e.EndPoint}, failure type:{e.FailureType}, connection type:{e.ConnectionType}");
        }

        private void Connection_ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            logger.LogError($"Server replied with error, {e.Message}, for endpoint:{e.EndPoint}");
        }
    }

    static class RedisConnectionExtensions
    {
        public static void LogEvents(this IConnectionMultiplexer connection, ILogger logger)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _ = new RedisEvents(connection, logger);
        }
    }
}

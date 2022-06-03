using System;

namespace DotNetCore.CAP.RedisStreams
{
    internal static class RedisErrorExtensions
    {
        public static RedisErrorTypes GetRedisErrorType(this string redisError)
        {
            if (string.Equals("BUSYGROUP Consumer Group name already exists", redisError, StringComparison.InvariantCultureIgnoreCase))
            {
                return RedisErrorTypes.GroupAlreadyExists;
            }

            if (string.Equals("ERR no such key", redisError, StringComparison.InvariantCultureIgnoreCase))
            {
                return RedisErrorTypes.NoGroupInfoExists;
            }

            return RedisErrorTypes.Unknown;
        }

        public static RedisErrorTypes GetRedisErrorType(this Exception exception)
            => exception.Message.GetRedisErrorType();
    }

    internal enum RedisErrorTypes : byte
    {
        Unknown = 0,
        GroupAlreadyExists = 1,
        NoGroupInfoExists = 2
    }
}

using System;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.RedisStreams
{
    interface IRedisConnectionPool
    {
        Task<IConnectionMultiplexer> ConnectAsync();
    }
}

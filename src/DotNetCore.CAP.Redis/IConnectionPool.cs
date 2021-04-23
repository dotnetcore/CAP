using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    interface IRedisConnectionPool
    {
        Task<IConnectionMultiplexer> ConnectAsync();
    }
}

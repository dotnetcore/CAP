using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Redis
{
    interface IRedisCacheManager : IDisposable
    {
        Task SubscribeAsync(string channelName, IEnumerable<string> topics, Func<RedisChannel,RedisMessage, Task> callback);
        Task PublishAsync(string channelName, RedisValue message);
        Task UnsubscribeAsync();
    }
}

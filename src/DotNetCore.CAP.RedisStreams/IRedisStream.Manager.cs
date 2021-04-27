using System;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.RedisStreams
{
    interface IRedisStreamManager
    {
        Task CreateStreamWithConsumerGroupAsync(string stream, string consumerGroup);
        Task PublishAsync(string stream, NameValueEntry[] message);
        IAsyncEnumerable<RedisStream[]> PollStreamsLatestMessagesAsync(string[] streams, string consumerGroup, TimeSpan pollDelay, CancellationToken token);
        IAsyncEnumerable<RedisStream[]> PollStreamsPendingMessagesAsync(string[] streams, string consumerGroup, TimeSpan pollDelay, CancellationToken token);        
        Task Ack(string stream, string consumerGroup, string messageId);
    }
}

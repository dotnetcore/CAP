// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams
{
    internal interface IRedisStreamManager
    {
        Task CreateStreamWithConsumerGroupAsync(string stream, string consumerGroup);
        Task PublishAsync(string stream, NameValueEntry[] message);

        IAsyncEnumerable<IEnumerable<RedisStream>> PollStreamsLatestMessagesAsync(string[] streams, string consumerGroup,
            TimeSpan pollDelay, CancellationToken token);

        IAsyncEnumerable<IEnumerable<RedisStream>> PollStreamsPendingMessagesAsync(string[] streams, string consumerGroup,
            TimeSpan pollDelay, CancellationToken token);

        Task Ack(string stream, string consumerGroup, string messageId);
    }
}
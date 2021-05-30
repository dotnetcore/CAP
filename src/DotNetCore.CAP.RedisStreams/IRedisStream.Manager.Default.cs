﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams
{
    internal class RedisStreamManager : IRedisStreamManager
    {
        private readonly IRedisConnectionPool _connectionsPool;
        private readonly ILogger<RedisStreamManager> _logger;
        private readonly CapRedisOptions _options;
        private IConnectionMultiplexer _redis;

        public RedisStreamManager(IRedisConnectionPool connectionsPool, IOptions<CapRedisOptions> options,
            ILogger<RedisStreamManager> logger)
        {
            _options = options.Value;
            _connectionsPool = connectionsPool;
            _logger = logger;
        }

        public async Task CreateStreamWithConsumerGroupAsync(string stream, string consumerGroup)
        {
            await ConnectAsync();

            //The object returned from GetDatabase is a cheap pass - thru object, and does not need to be stored
            var database = _redis.GetDatabase();
            var streamExist = await database.KeyTypeAsync(stream);
            if (streamExist == RedisType.None)
            {
                await database.StreamCreateConsumerGroupAsync(stream, consumerGroup, StreamPosition.NewMessages);
            }
            else
            {
                var groupInfo = await database.StreamGroupInfoAsync(stream);
                if (groupInfo.Any(g => g.Name == consumerGroup))
                    return;
                await database.StreamCreateConsumerGroupAsync(stream, consumerGroup, StreamPosition.NewMessages);
            }
        }

        public async Task PublishAsync(string stream, NameValueEntry[] message)
        {
            await ConnectAsync();

            //The object returned from GetDatabase is a cheap pass - thru object, and does not need to be stored
            await _redis.GetDatabase().StreamAddAsync(stream, message);
        }

        public async IAsyncEnumerable<RedisStream[]> PollStreamsLatestMessagesAsync(string[] streams,
            string consumerGroup, TimeSpan pollDelay, [EnumeratorCancellation] CancellationToken token)
        {
            var positions = streams.Select(stream => new StreamPosition(stream, StreamPosition.NewMessages));

            while (true)
            {
                var result = await TryReadConsumerGroup(consumerGroup, positions.ToArray(), token)
                    .ConfigureAwait(false);

                yield return result.streams;

                token.WaitHandle.WaitOne(pollDelay);
            }
        }

        public async IAsyncEnumerable<RedisStream[]> PollStreamsPendingMessagesAsync(string[] streams,
            string consumerGroup, TimeSpan pollDelay, [EnumeratorCancellation] CancellationToken token)
        {
            var positions = streams.Select(stream => new StreamPosition(stream, StreamPosition.Beginning));

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var result = await TryReadConsumerGroup(consumerGroup, positions.ToArray(), token)
                    .ConfigureAwait(false);

                yield return result.streams;

                //Once we consumed our history of pending messages, we can break the loop.
                if (result.canRead && result.streams.All(s => s.Entries.Length < _options.StreamEntriesCount))
                    break;

                token.WaitHandle.WaitOne(pollDelay);
            }
        }

        public async Task Ack(string stream, string consumerGroup, string messageId)
        {
            await ConnectAsync();

            await _redis.GetDatabase().StreamAcknowledgeAsync(stream, consumerGroup, messageId).ConfigureAwait(false);
        }

        private async Task<(bool canRead, RedisStream[] streams)> TryReadConsumerGroup(string consumerGroup,
            StreamPosition[] positions, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var createdPositions = new List<StreamPosition>();

                await ConnectAsync();

                var database = _redis.GetDatabase();

                await foreach (var position in database.TryCreateConsumerGroup(positions, consumerGroup, _logger)
                    .WithCancellation(token))
                    createdPositions.Add(position);

                if (!createdPositions.Any()) return (false, Array.Empty<RedisStream>());

                var readSet = database.StreamReadGroupAsync(createdPositions.ToArray(), consumerGroup, consumerGroup,
                    (int) _options.StreamEntriesCount);

                return (true, await readSet.ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Redis error when trying read consumer group {consumerGroup}");
                return (false, Array.Empty<RedisStream>());
            }
        }

        private async Task ConnectAsync()
        {
            _redis = await _connectionsPool.ConnectAsync();
        }
    }
}
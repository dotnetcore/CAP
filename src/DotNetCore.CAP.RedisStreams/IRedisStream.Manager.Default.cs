// Copyright (c) .NET Core Community. All rights reserved.
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
        private IConnectionMultiplexer? _redis;

        public RedisStreamManager(IRedisConnectionPool connectionsPool, IOptions<CapRedisOptions> options,
            ILogger<RedisStreamManager> logger)
        {
            _options = options.Value;
            _connectionsPool = connectionsPool;
            _logger = logger;
        }

        public async Task CreateStreamWithConsumerGroupAsync(string stream, string consumerGroup)
        {
            await ConnectAsync()
                .ConfigureAwait(false);

            //The object returned from GetDatabase is a cheap pass - thru object, and does not need to be stored
            var database = _redis!.GetDatabase();

            await database.TryGetOrCreateStreamConsumerGroupAsync(stream, consumerGroup)
                .ConfigureAwait(false);
        }

        public async Task PublishAsync(string stream, NameValueEntry[] message)
        {
            await ConnectAsync()
                .ConfigureAwait(false);

            //The object returned from GetDatabase is a cheap pass - thru object, and does not need to be stored
            await _redis!.GetDatabase().StreamAddAsync(stream, message)
                .ConfigureAwait(false);
        }

        public async IAsyncEnumerable<IEnumerable<RedisStream>> PollStreamsLatestMessagesAsync(string[] streams,
            string consumerGroup, TimeSpan pollDelay, [EnumeratorCancellation] CancellationToken token)
        {
            var positions = streams.Select(stream => new StreamPosition(stream, StreamPosition.NewMessages));

            while (true)
            {
                var result = await TryReadConsumerGroupAsync(consumerGroup, positions.ToArray(), token)
                    .ConfigureAwait(false);

                yield return result;

                token.WaitHandle.WaitOne(pollDelay);
            }
        }

        public async IAsyncEnumerable<IEnumerable<RedisStream>> PollStreamsPendingMessagesAsync(string[] streams,
            string consumerGroup, TimeSpan pollDelay, [EnumeratorCancellation] CancellationToken token)
        {
            var positions = streams.Select(stream => new StreamPosition(stream, StreamPosition.Beginning));

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var result = await TryReadConsumerGroupAsync(consumerGroup, positions.ToArray(), token)
                    .ConfigureAwait(false);

                yield return result;

                //Once we consumed our history of pending messages, we can break the loop.
                if (result.All(s => s.Entries.Length < _options.StreamEntriesCount))
                    break;

                token.WaitHandle.WaitOne(pollDelay);
            }
        }

        public async Task Ack(string stream, string consumerGroup, string messageId)
        {
            await ConnectAsync()
                .ConfigureAwait(false);

            await _redis!.GetDatabase().StreamAcknowledgeAsync(stream, consumerGroup, messageId)
                .ConfigureAwait(false);
        }

        private async Task<IEnumerable<RedisStream>> TryReadConsumerGroupAsync(string consumerGroup,
            StreamPosition[] positions, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var createdPositions = new List<StreamPosition>();

                await ConnectAsync()
                    .ConfigureAwait(false);

                var database = _redis!.GetDatabase();

                await foreach (var position in database.TryGetOrCreateConsumerGroupPositionsAsync(positions, consumerGroup, _logger)
                    .ConfigureAwait(false).WithCancellation(token))
                {
                    createdPositions.Add(position);
                }

                if (!createdPositions.Any()) return Array.Empty<RedisStream>();

                //calculate keys HashSlots to start reading per HashSlot
                var groupedPositions = createdPositions.GroupBy(s => _redis.GetHashSlot(s.Key))
                    .Select(group => database.StreamReadGroupAsync(group.ToArray(), consumerGroup, consumerGroup, (int)_options.StreamEntriesCount));

                var readSet = await Task.WhenAll(groupedPositions)
                    .ConfigureAwait(false);

                return readSet.SelectMany(set => set);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Redis error when trying read consumer group {consumerGroup}");
            }

            return Array.Empty<RedisStream>();
        }

        private async Task ConnectAsync()
        {
            _redis = await _connectionsPool.ConnectAsync()
                .ConfigureAwait(false);
        }
    }
}
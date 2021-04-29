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
    class RedisStreamManager : IRedisStreamManager
    {
        private readonly CapRedisOptions options;
        private readonly IRedisConnectionPool connectionsPool;
        private readonly ILogger<RedisStreamManager> logger;
        private IConnectionMultiplexer redis;

        public RedisStreamManager(IRedisConnectionPool connectionsPool, IOptions<CapRedisOptions> options, ILogger<RedisStreamManager> logger)
        {
            this.options = options.Value;
            this.connectionsPool = connectionsPool;
            this.logger = logger;
        }

        public async Task CreateStreamWithConsumerGroupAsync(string stream, string consumerGroup)
        {
            await ConnectAsync();

            //The object returned from GetDatabase is a cheap pass - thru object, and does not need to be stored
            var database = redis.GetDatabase();
            var streamExist = await database.KeyTypeAsync(stream);
            if (streamExist == RedisType.None)
            {
                await database.StreamCreateConsumerGroupAsync(stream, consumerGroup, StreamPosition.NewMessages, true);
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
            await redis.GetDatabase().StreamAddAsync(stream, message);
        }

        public async IAsyncEnumerable<RedisStream[]> PollStreamsLatestMessagesAsync(string[] streams, string consumerGroup, TimeSpan pollDelay, [EnumeratorCancellation] CancellationToken token)
        {
            var positions = streams.Select(stream => new StreamPosition(stream, StreamPosition.NewMessages));

            while (true)
            {
                var result = await TryReadConsumerGroup(consumerGroup, positions.ToArray(), token).ConfigureAwait(false);

                yield return result.streams;

                token.WaitHandle.WaitOne(pollDelay);
            }
        }

        public async IAsyncEnumerable<RedisStream[]> PollStreamsPendingMessagesAsync(string[] streams, string consumerGroup, TimeSpan pollDelay, [EnumeratorCancellation] CancellationToken token)
        {
            var positions = streams.Select(stream => new StreamPosition(stream, StreamPosition.Beginning));

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var result = await TryReadConsumerGroup(consumerGroup, positions.ToArray(), token).ConfigureAwait(false);

                yield return result.streams;

                //Once we consumed our history of pending messages, we can break the loop.
                if (result.canRead && result.streams.All(s => s.Entries.Length < options.StreamEntriesCount))
                    break;

                token.WaitHandle.WaitOne(pollDelay);
            }
        }

        private async Task<(bool canRead, RedisStream[] streams)> TryReadConsumerGroup(string consumerGroup, StreamPosition[] positions, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var createdPositions = new List<StreamPosition>();

                await ConnectAsync();

                var database = redis.GetDatabase();

                await foreach (var position in database.TryCreateConsumerGroup(positions, consumerGroup, logger))
                {
                    createdPositions.Add(position);
                }

                if (!createdPositions.Any()) return (false, Array.Empty<RedisStream>());

                var readSet = database.StreamReadGroupAsync(createdPositions.ToArray(), consumerGroup, consumerGroup, (int)options.StreamEntriesCount);

                return (true, await readSet.ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Redis error when trying read consumer group {consumerGroup}");
                return (false, Array.Empty<RedisStream>());
            }
        }

        public async Task Ack(string stream, string consumerGroup, string messageId)
        {
            await ConnectAsync();

            await redis.GetDatabase().StreamAcknowledgeAsync(stream, consumerGroup, messageId).ConfigureAwait(false);
        }

        private async Task ConnectAsync()
        {
            redis = await connectionsPool.ConnectAsync();
        }
    }
}

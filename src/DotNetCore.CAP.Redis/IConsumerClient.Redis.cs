using DotNetCore.CAP.Redis;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DotNetCore.CAP.Redis
{
    class RedisConsumerClient : IConsumerClient
    {
        private readonly ILogger<RedisConsumerClient> logger;
        private readonly IRedisStreamManager redis;
        private readonly CapRedisOptions options;
        private readonly string groupId;
        private string[] topics;

        public RedisConsumerClient(
            string groubId,
            IRedisStreamManager redis,
            CapRedisOptions options,
            ILogger<RedisConsumerClient> logger
            )
        {
            this.groupId = groubId;
            this.redis = redis;
            this.options = options;
            this.logger = logger;
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("redis", options.Endpoint);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null) throw new ArgumentNullException(nameof(topics));

            foreach (var topic in topics)
            {
                redis.CreateStreamWithConsumerGroupAsync(topic, groupId).GetAwaiter().GetResult();
            }

            this.topics = topics.ToArray();
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            _ = ListeningForMessagesAsync(timeout, cancellationToken);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
        }

        private async Task ListeningForMessagesAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            //first time, we want to read our pending messages, in case we crashed and are recovering.
            var pendingMsgs = redis.PollStreamsPendingMessagesAsync(topics, groupId, timeout, cancellationToken);

            await ConsumeMessages(pendingMsgs, StreamPosition.Beginning);

            //Once we consumed our history, we can start getting new messages.
            var newMsgs = redis.PollStreamsLatestMessagesAsync(topics, groupId, timeout, cancellationToken);

            _ = ConsumeMessages(newMsgs, StreamPosition.NewMessages);
        }

        private async Task ConsumeMessages(IAsyncEnumerable<RedisStream[]> streamsSet, RedisValue position)
        {
            await foreach (var set in streamsSet)
            {
                foreach (var stream in set)
                {
                    foreach (var entry in stream.Entries)
                    {
                        if (entry.IsNull)
                            return;
                        try
                        {
                            var message = RedisMessage.Create(entry, groupId);
                            OnMessageReceived?.Invoke((stream.Key.ToString(), groupId, entry.Id.ToString()), message);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.Message, ex);
                            var logArgs = new LogMessageEventArgs
                            {
                                LogType = MqLogType.ConsumeError,
                                Reason = ex.ToString()
                            };
                            OnLog?.Invoke(entry, logArgs);
                        }
                        finally
                        {
                            string positionName = position == StreamPosition.Beginning ? nameof(StreamPosition.Beginning) : nameof(StreamPosition.NewMessages);
                            logger.LogDebug($"Redis stream entry [{entry.Id}] [position : {positionName}] was delivered.");
                        }
                    }
                }
            }
        }

        public void Commit(object sender)
        {
            var (stream, group, id) = ((string stream, string group, string id))sender;

            redis.Ack(stream, group, id).GetAwaiter().GetResult();
        }

        public void Reject(object sender)
        {
            // ignore
        }

        public void Dispose()
        {
            //ignore
        }

    }
}

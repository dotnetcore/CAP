﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams;

internal class RedisConsumerClient(
    string _groupId,
    byte _groupConcurrent,
    IRedisStreamManager _redis,
    IOptions<CapRedisOptions> _options,
    ILogger<RedisConsumerClient> _logger
    ) : IConsumerClient
{
    private readonly SemaphoreSlim _semaphore = new(_groupConcurrent);
    private string[] _topics = default!;

    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

    public BrokerAddress BrokerAddress => new("redis", _options.Value.Endpoint);

    public void Subscribe(IEnumerable<string> topics)
    {
        ArgumentNullException.ThrowIfNull(topics);

        foreach (var topic in topics)
        {
            _redis.CreateStreamWithConsumerGroupAsync(topic, _groupId).GetAwaiter().GetResult();
        }

        _topics = topics.ToArray();
    }

    public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
    {
        _ = ListeningForMessagesAsync(timeout, cancellationToken);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.WaitHandle.WaitOne(timeout);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public void Commit(object? sender)
    {
        var (stream, group, id) = ((string stream, string group, string id))sender!;

        _redis.Ack(stream, group, id).GetAwaiter().GetResult();

        _semaphore.Release();
    }

    public void Reject(object? sender)
    {
        _semaphore.Release();
    }

    public void Dispose()
    {
        //ignore
    }

    private async Task ListeningForMessagesAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        //first time, we want to read our pending messages, in case we crashed and are recovering.
        var pendingMsgs = _redis.PollStreamsPendingMessagesAsync(_topics, _groupId, timeout, cancellationToken);

        await ConsumeMessages(pendingMsgs, StreamPosition.Beginning)
            .ConfigureAwait(false);

        //Once we consumed our history, we can start getting new messages.
        var newMsgs = _redis.PollStreamsLatestMessagesAsync(_topics, _groupId, timeout, cancellationToken);

        _ = ConsumeMessages(newMsgs, StreamPosition.NewMessages);
    }

    private async Task ConsumeMessages(IAsyncEnumerable<IEnumerable<RedisStream>> streamsSet, RedisValue position)
    {
        await foreach (var set in streamsSet)
        {
            foreach (var stream in set)
            {
                foreach (var entry in stream.Entries)
                {
                    if (entry.IsNull) return;

                    if (_groupConcurrent > 0)
                    {
                        _semaphore.Wait();
                        _ = Task.Run(() => Consume(position, stream, entry)).ConfigureAwait(false);
                    }
                    else
                    {
                        await Consume(position, stream, entry);
                    }
                }
            }
        }

        async Task Consume(RedisValue position, RedisStream stream, StreamEntry entry)
        {
            try
            {
                var message = RedisMessage.Create(entry, _groupId);
                await OnMessageCallback!(message, (stream.Key.ToString(), _groupId, entry.Id.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, message: "Redis entry {entryId} on stream {streamKey} at position {position} of group {groupId} is not valid for Cap, see inner exception for more details.", entry.Id, stream.Key, position, _groupId);

                var logArgs = new LogMessageEventArgs
                {
                    LogType = MqLogType.RedisConsumeError,
                    Reason = ex.ToString()
                };

                try
                {
                    var onError = _options.Value.OnConsumeError?.Invoke(new CapRedisOptions.ConsumeErrorContext(ex, entry));

                    await (onError ?? Task.CompletedTask).ConfigureAwait(false);
                }
                catch (Exception onError)
                {
                    _logger.LogError(onError, "Unhandled exception occurred in {action} action, Exception has been caught.", nameof(CapRedisOptions.OnConsumeError));
                }
                finally
                {
                    OnLogCallback!(logArgs);
                }
            }
            finally
            {
                var positionName = position == StreamPosition.Beginning
                    ? nameof(StreamPosition.Beginning)
                    : nameof(StreamPosition.NewMessages);
                _logger.LogDebug("Redis stream entry [{entryId}] [position : {positionName}] was delivered.", entry.Id, positionName);
            }
        }
    }
}
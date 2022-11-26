// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor;

public class Dispatcher : IDispatcher
{
    private CancellationTokenSource? _tasksCts;
    private readonly CancellationTokenSource _delayCts = new();
    private readonly ISubscribeExecutor _executor;
    private readonly ILogger<Dispatcher> _logger;
    private readonly CapOptions _options;
    private readonly IMessageSender _sender;
    private readonly IDataStorage _storage;
    private readonly PriorityQueue<MediumMessage, long> _schedulerQueue;
    private readonly bool _enablePrefetch;

    private Channel<MediumMessage> _publishedChannel = default!;
    private Channel<(MediumMessage, ConsumerExecutorDescriptor?)> _receivedChannel = default!;
    private long _nextSendTime = DateTime.MaxValue.Ticks;

    public Dispatcher(ILogger<Dispatcher> logger,
        IMessageSender sender,
        IOptions<CapOptions> options,
        ISubscribeExecutor executor,
        IDataStorage storage)
    {
        _logger = logger;
        _sender = sender;
        _options = options.Value;
        _executor = executor;
        _schedulerQueue = new PriorityQueue<MediumMessage, long>();
        _storage = storage;
        _enablePrefetch = options.Value.EnableConsumerPrefetch;
    }

    public async Task Start(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        _tasksCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, CancellationToken.None);
        _tasksCts.Token.Register(() => _delayCts.Cancel());

        var capacity = _options.ProducerThreadCount * 500;
        _publishedChannel = Channel.CreateBounded<MediumMessage>(
            new BoundedChannelOptions(capacity > 5000 ? 5000 : capacity)
            {
                AllowSynchronousContinuations = true,
                SingleReader = _options.ProducerThreadCount == 1,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        await Task.WhenAll(Enumerable.Range(0, _options.ProducerThreadCount)
            .Select(_ => Task.Factory.StartNew(Sending, _tasksCts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray()).ConfigureAwait(false);

        if (_enablePrefetch)
        {
            capacity = _options.ConsumerThreadCount * 300;
            _receivedChannel = Channel.CreateBounded<(MediumMessage, ConsumerExecutorDescriptor?)>(
                new BoundedChannelOptions(capacity > 3000 ? 3000 : capacity)
                {
                    AllowSynchronousContinuations = true,
                    SingleReader = _options.ConsumerThreadCount == 1,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });

            await Task.WhenAll(Enumerable.Range(0, _options.ConsumerThreadCount)
                .Select(_ => Task.Factory.StartNew(Processing, _tasksCts.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray()).ConfigureAwait(false);
        }

        _ = Task.Factory.StartNew(async () =>
        {
            //When canceling, place the message status of unsent in the queue to delayed
            _tasksCts.Token.Register(() =>
            {
                try
                {
                    if (_schedulerQueue.Count == 0) return;

                    var messageIds = _schedulerQueue.UnorderedItems.Select(x => x.Element.DbId).ToArray();
                    _storage.ChangePublishStateToDelayedAsync(messageIds).GetAwaiter().GetResult();
                    _logger.LogDebug("Update storage to delayed success of delayed message in memory queue!");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Update storage fails of delayed message in memory queue!");
                }
            });

            while (!_tasksCts.Token.IsCancellationRequested)
            {
                try
                {
                    while (_schedulerQueue.TryPeek(out _, out _nextSendTime))
                    {
                        var delayTime = _nextSendTime - DateTime.Now.Ticks;

                        if (delayTime > 500000) //50ms
                        {
                            await Task.Delay(new TimeSpan(delayTime), _delayCts.Token);
                        }
                        _tasksCts.Token.ThrowIfCancellationRequested();

                        await _sender.SendAsync(_schedulerQueue.Dequeue()).ConfigureAwait(false);
                    }
                    _tasksCts.Token.WaitHandle.WaitOne(100);
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
            }
        }, _tasksCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);

        _logger.LogInformation("Starting default Dispatcher");
    }

    public async ValueTask EnqueueToScheduler(MediumMessage message, DateTime publishTime, object? transaction = null)
    {
        message.ExpiresAt = publishTime;

        var timeSpan = publishTime - DateTime.Now;

        if (timeSpan <= TimeSpan.FromMinutes(1)) //1min
        {
            await _storage.ChangePublishStateAsync(message, StatusName.Queued, transaction);

            _schedulerQueue.Enqueue(message, publishTime.Ticks);

            if (publishTime.Ticks < _nextSendTime)
            {
                _delayCts.Cancel();
            }
        }
        else
        {
            await _storage.ChangePublishStateAsync(message, StatusName.Delayed, transaction);
        }
    }

    public async ValueTask EnqueueToPublish(MediumMessage message)
    {
        try
        {
            if (!_publishedChannel.Writer.TryWrite(message))
                while (await _publishedChannel.Writer.WaitToWriteAsync(_tasksCts!.Token).ConfigureAwait(false))
                    if (_publishedChannel.Writer.TryWrite(message))
                        return;
        }
        catch (OperationCanceledException)
        {
            //Ignore
        }
    }

    public async ValueTask EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor? descriptor = null)
    {
        try
        {
            if (_enablePrefetch)
            {
                if (!_receivedChannel.Writer.TryWrite((message, descriptor)))
                {
                    while (await _receivedChannel.Writer.WaitToWriteAsync(_tasksCts!.Token).ConfigureAwait(false))
                        if (_receivedChannel.Writer.TryWrite((message, descriptor)))
                            return;
                }
            }
            else
            {
                await _executor.ExecuteAsync(message, descriptor, _tasksCts!.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            //Ignore
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"An exception occurred when invoke subscriber. MessageId:{message.DbId}");
        }
    }

    public void Dispose()
    {
        _tasksCts?.Dispose();
    }

    private async ValueTask Sending()
    {
        try
        {
            while (await _publishedChannel.Reader.WaitToReadAsync(_tasksCts!.Token).ConfigureAwait(false))
                while (_publishedChannel.Reader.TryRead(out var message))
                    try
                    {
                        var result = await _sender.SendAsync(message).ConfigureAwait(false);
                        if (!result.Succeeded)
                            _logger.MessagePublishException(message.Origin.GetId(), result.ToString(), result.Exception);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            $"An exception occurred when sending a message to the MQ. Id:{message.DbId}");
                    }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    private async ValueTask Processing()
    {
        try
        {
            while (await _receivedChannel.Reader.WaitToReadAsync(_tasksCts!.Token).ConfigureAwait(false))
                while (_receivedChannel.Reader.TryRead(out var message))
                    try
                    {
                        await _executor.ExecuteAsync(message.Item1, message.Item2, _tasksCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        //expected
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e,
                            $"An exception occurred when invoke subscriber. MessageId:{message.Item1.DbId}");
                    }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }
}
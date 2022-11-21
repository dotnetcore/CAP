// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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

internal class DispatcherPerGroup : IDispatcher
{
    private CancellationTokenSource? _tasksCTS;
    private readonly CancellationTokenSource _delayCTS = new();
    private readonly ISubscribeDispatcher _executor;
    private readonly ILogger<Dispatcher> _logger;
    private readonly CapOptions _options;
    private readonly IMessageSender _sender;
    private readonly IDataStorage _storage;
    private readonly PriorityQueue<MediumMessage, DateTime> _schedulerQueue;

    private Channel<MediumMessage> _publishedChannel = default!;
    private ConcurrentDictionary<string, Channel<(MediumMessage, ConsumerExecutorDescriptor)>> _receivedChannels = default!;

    private DateTime _nextSendTime = DateTime.MaxValue;

    public DispatcherPerGroup(ILogger<Dispatcher> logger,
        IMessageSender sender,
        IOptions<CapOptions> options,
        ISubscribeDispatcher executor,
        IDataStorage storage)
    {
        _logger = logger;
        _sender = sender;
        _options = options.Value;
        _executor = executor;
        _schedulerQueue = new PriorityQueue<MediumMessage, DateTime>();
        _storage = storage;
    }

    public async Task Start(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        _tasksCTS = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, CancellationToken.None);
        _tasksCTS.Token.Register(() => _delayCTS.Cancel());

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
            .Select(_ => Task.Factory.StartNew(() => Sending(), _tasksCTS.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

        _receivedChannels =
            new ConcurrentDictionary<string, Channel<(MediumMessage, ConsumerExecutorDescriptor)>>(
                _options.ConsumerThreadCount, _options.ConsumerThreadCount * 2);

        GetOrCreateReceiverChannel(_options.DefaultGroupName);

        await Task.Factory.StartNew(async () =>
        {
            while (!_tasksCTS.Token.IsCancellationRequested)
            {
                try
                {
                    while (_schedulerQueue.TryPeek(out var message, out _nextSendTime))
                    {
                        var delayTime = _nextSendTime - DateTime.Now;

                        if (delayTime > new TimeSpan(500000)) //50ms
                        {
                            await Task.Delay(delayTime, _delayCTS.Token);
                        }
                        _tasksCTS.Token.ThrowIfCancellationRequested();

                        await _sender.SendAsync(_schedulerQueue.Dequeue()).ConfigureAwait(false);
                    }
                    _tasksCTS.Token.WaitHandle.WaitOne(100);
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
            }
        }, _tasksCTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);

        _logger.LogInformation("Starting DispatcherPerGroup");
    }

    public async ValueTask EnqueueToScheduler(MediumMessage message, DateTime publishTime)
    {
        message.ExpiresAt = publishTime;

        var timeSpan = publishTime - DateTime.Now;

        if (timeSpan <= TimeSpan.FromMinutes(1))
        {
            await _storage.ChangePublishStateAsync(message, StatusName.Queued);

            _schedulerQueue.Enqueue(message, publishTime);

            if (publishTime < _nextSendTime)
            {
                _delayCTS.Cancel();
            }
        }
        else
        {
            await _storage.ChangePublishStateAsync(message, StatusName.Delayed);
        }
    }

    public async ValueTask EnqueueToPublish(MediumMessage message)
    {
        try
        {
            if (!_publishedChannel.Writer.TryWrite(message))
                while (await _publishedChannel.Writer.WaitToWriteAsync(_tasksCTS!.Token).ConfigureAwait(false))
                    if (_publishedChannel.Writer.TryWrite(message))
                        return;
        }
        catch (OperationCanceledException)
        {
            //Ignore
        }
    }

    public async ValueTask EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor)
    {
        try
        {
            var group = descriptor.Attribute.Group ?? _options.DefaultGroupName;

            if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("Enqueue message for group {ConsumerGroup}", group);

            var channel = GetOrCreateReceiverChannel(group);

            if (!channel.Writer.TryWrite((message, descriptor)))
                while (await channel.Writer.WaitToWriteAsync(_tasksCTS!.Token).ConfigureAwait(false))
                    if (channel.Writer.TryWrite((message, descriptor)))
                        return;
        }
        catch (OperationCanceledException)
        {
            //Ignore
        }
    }

    public void Dispose()
    {
        _tasksCTS?.Cancel();
        _tasksCTS?.Dispose();
        _tasksCTS = null;
    }

    private Channel<(MediumMessage, ConsumerExecutorDescriptor)> GetOrCreateReceiverChannel(string key)
    {
        return _receivedChannels.GetOrAdd(key, group =>
        {
            _logger.LogInformation(
                "Creating receiver channel for group {ConsumerGroup} with thread count {ConsumerThreadCount}", group,
                _options.ConsumerThreadCount);

            var capacity = _options.ConsumerThreadCount * 300;
            var channel = Channel.CreateBounded<(MediumMessage, ConsumerExecutorDescriptor)>(
                new BoundedChannelOptions(capacity > 3000 ? 3000 : capacity)
                {
                    AllowSynchronousContinuations = true,
                    SingleReader = _options.ConsumerThreadCount == 1,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });

            Task.WhenAll(Enumerable.Range(0, _options.ConsumerThreadCount)
                .Select(_ => Task.Factory.StartNew(() => Processing(group, channel), _tasksCTS!.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

            return channel;
        });
    }

    private async Task Sending()
    {
        try
        {
            while (await _publishedChannel.Reader.WaitToReadAsync(_tasksCTS!.Token).ConfigureAwait(false))
                while (_publishedChannel.Reader.TryRead(out var message))
                    try
                    {
                        var result = await _sender.SendAsync(message).ConfigureAwait(false);
                        if (!result.Succeeded)
                            _logger.MessagePublishException(
                                message.Origin.GetId(),
                                result.ToString(),
                                result.Exception);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An exception occurred when sending a message to the MQ. Id:{message.DbId}");
                    }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    private async Task Processing(string group, Channel<(MediumMessage, ConsumerExecutorDescriptor)> channel)
    {
        try
        {
            while (await channel.Reader.WaitToReadAsync(_tasksCTS!.Token).ConfigureAwait(false))
                while (channel.Reader.TryRead(out var message))
                    try
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug("Dispatching message for group {ConsumerGroup}", group);

                        await _executor.DispatchAsync(message.Item1, message.Item2, _tasksCTS.Token).ConfigureAwait(false);
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
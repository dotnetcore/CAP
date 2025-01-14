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
    private readonly ISubscribeExecutor _executor;
    private readonly ILogger<Dispatcher> _logger;
    private readonly CapOptions _options;
    private readonly IMessageSender _sender;
    private readonly IDataStorage _storage;
    private readonly ScheduledMediumMessageQueue _schedulerQueue = new();
    private readonly bool _enableParallelExecute;
    private readonly bool _enableParallelSend;
    private readonly int _pChannelSize;

    private CancellationTokenSource? _tasksCts;
    private Channel<MediumMessage> _publishedChannel = default!;
    private Channel<(MediumMessage, ConsumerExecutorDescriptor?)> _receivedChannel = default!;

    public Dispatcher(ILogger<Dispatcher> logger, IMessageSender sender, IOptions<CapOptions> options,
        ISubscribeExecutor executor, IDataStorage storage)
    {
        _logger = logger;
        _sender = sender;
        _options = options.Value;
        _executor = executor;
        _storage = storage;
        _enableParallelExecute = options.Value.EnableSubscriberParallelExecute;
        _enableParallelSend = options.Value.EnablePublishParallelSend;
        _pChannelSize = Environment.ProcessorCount * 500;
    }

    public async Task Start(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        _tasksCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, CancellationToken.None);

        _publishedChannel = Channel.CreateBounded<MediumMessage>(new BoundedChannelOptions(_pChannelSize)
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        await Task.Run(Sending, _tasksCts.Token).ConfigureAwait(false); //here return valuetask

        if (_enableParallelExecute)
        {
            _receivedChannel = Channel.CreateBounded<(MediumMessage, ConsumerExecutorDescriptor?)>(
                new BoundedChannelOptions(_options.SubscriberParallelExecuteThreadCount * _options.SubscriberParallelExecuteBufferFactor)
                {
                    AllowSynchronousContinuations = true,
                    SingleReader = _options.SubscriberParallelExecuteThreadCount == 1,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });

            await Task.WhenAll(Enumerable.Range(0, _options.SubscriberParallelExecuteThreadCount)
                .Select(_ => Task.Run(Processing, _tasksCts.Token)).ToArray())
                .ConfigureAwait(false);
        }
        _ = Task.Run(async () =>
        {
            //When canceling, place the message status of unsent in the queue to delayed
            _tasksCts.Token.Register(() =>
            {
                try
                {
                    if (_schedulerQueue.Count == 0) return;

                    var messageIds = _schedulerQueue.UnorderedItems.Select(x => x.DbId).ToArray();
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
                    await foreach (var nextMessage in _schedulerQueue.GetConsumingEnumerable(_tasksCts.Token))
                    {
                        _tasksCts.Token.ThrowIfCancellationRequested();
                        await _sender.SendAsync(nextMessage).ConfigureAwait(false);
                    }

                    _tasksCts.Token.WaitHandle.WaitOne(100);
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "Scheduled message publishing failed unexpectedly, which will stop future scheduled " +
                        "messages from publishing. See more details here: https://github.com/dotnetcore/CAP/issues/1637. " +
                        "Exception: {Message}", 
                        ex.Message);
                    throw;
                }
            }
        }, _tasksCts.Token).ConfigureAwait(false);
    }

    public async Task EnqueueToScheduler(MediumMessage message, DateTime publishTime, object? transaction = null)
    {
        message.ExpiresAt = publishTime;

        var timeSpan = publishTime - DateTime.Now;

        if (timeSpan <= TimeSpan.FromMinutes(1)) //1min
        {
            await _storage.ChangePublishStateAsync(message, StatusName.Queued, transaction);

            _schedulerQueue.Enqueue(message, publishTime.Ticks);
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
            if (_tasksCts!.IsCancellationRequested) return;

            if (_enableParallelSend && message.Retries == 0)
            {
                if (!_publishedChannel.Writer.TryWrite(message))
                    while (await _publishedChannel.Writer.WaitToWriteAsync(_tasksCts!.Token).ConfigureAwait(false))
                        if (_publishedChannel.Writer.TryWrite(message))
                            return;
            }
            else
            {
                var result = await _sender.SendAsync(message).ConfigureAwait(false);
                if (!result.Succeeded) _logger.MessagePublishException(message.Origin.GetId(), result.ToString(), result.Exception);

            }
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
            if (_tasksCts!.IsCancellationRequested) return;

            if (_enableParallelExecute && message.Retries == 0)
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
            _logger.LogError(e, "An exception occurred when invoke subscriber. MessageId:{MessageId}", message.DbId);
        }
    }

    public void Dispose()
    {
        _tasksCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async ValueTask Sending()
    {
        try
        {
            while (await _publishedChannel.Reader.WaitToReadAsync(_tasksCts!.Token).ConfigureAwait(false))
            {
                if (_enableParallelSend)
                {
                    var tasks = new List<Task>();
                    var batchSize = _pChannelSize / 50;
                    for (var i = 0; i < batchSize && _publishedChannel.Reader.TryRead(out var message); i++)
                    {
                        var item = message;
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var result = await _sender.SendAsync(item).ConfigureAwait(false);
                                if (!result.Succeeded) _logger.MessagePublishException(item.Origin.GetId(), result.ToString(), result.Exception);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"An exception occurred when sending a message to the transport. Id:{message.DbId}");
                            }
                        }));
                    }

                    await Task.WhenAll(tasks);
                }
                else
                {
                    while (_publishedChannel.Reader.TryRead(out var message))
                        try
                        {
                            var result = await _sender.SendAsync(message).ConfigureAwait(false);
                            if (!result.Succeeded) _logger.MessagePublishException(message.Origin.GetId(), result.ToString(), result.Exception);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred when sending a message to the transport. Id:{message.DbId}");
                        }
                }
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
                        var item1 = message.Item1;
                        var item2 = message.Item2;
                        await _executor.ExecuteAsync(item1, item2, _tasksCts.Token).ConfigureAwait(false);
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
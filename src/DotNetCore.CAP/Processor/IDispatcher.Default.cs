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
    private readonly int _publishChannelSize;

    private CancellationTokenSource? _tasksCts;
    private Channel<MediumMessage> _publishedChannel = default!;
    private Channel<(MediumMessage, ConsumerExecutorDescriptor?)> _receivedChannel = default!;
    private bool _disposed;

    public Dispatcher(
        ILogger<Dispatcher> logger,
        IMessageSender sender,
        IOptions<CapOptions> options,
        ISubscribeExecutor executor,
        IDataStorage storage)
    {
        _logger = logger;
        _sender = sender;
        _options = options.Value;
        _executor = executor;
        _storage = storage;
        _enableParallelExecute = _options.EnableSubscriberParallelExecute;
        _enableParallelSend = _options.EnablePublishParallelSend;
        _publishChannelSize = Environment.ProcessorCount * 500;
    }

    #region Public Methods

    public async Task Start(CancellationToken stoppingToken)
    {
        ResetStateIfNeeded();

        stoppingToken.ThrowIfCancellationRequested();
        _tasksCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, CancellationToken.None);

        InitializePublishedChannel();
        await StartSendingTaskAsync().ConfigureAwait(false);

        if (_enableParallelExecute)
        {
            InitializeReceivedChannel();
            await StartProcessingTasksAsync().ConfigureAwait(false);
        }

        _ = StartSchedulerTaskAsync().ConfigureAwait(false);
    }

    public async Task EnqueueToScheduler(MediumMessage message, DateTime publishTime, object? transaction = null)
    {
        message.ExpiresAt = publishTime;

        var timeSpan = publishTime - DateTime.Now;
        var statusName = timeSpan <= TimeSpan.FromMinutes(1) ? StatusName.Queued : StatusName.Delayed;

        await _storage.ChangePublishStateAsync(message, statusName, transaction).ConfigureAwait(false);

        if (statusName == StatusName.Queued)
        {
            _schedulerQueue.Enqueue(message, publishTime.Ticks);
        }
    }

    public async ValueTask EnqueueToPublish(MediumMessage message)
    {
        try
        {
            if (IsCancellationRequested())
            {
                _logger.LogWarning("The message has been persisted, but CAP is currently stopped. It will be attempted to be sent once CAP becomes available.");
                return;
            }

            if (ShouldUseParallelSend(message))
            {
                await WriteToChannelAsync(_publishedChannel, message).ConfigureAwait(false);
            }
            else
            {
                await SendMessageDirectlyAsync(message).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    public async ValueTask EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor? descriptor = null)
    {
        try
        {
            if (IsCancellationRequested())
            {
                return;
            }

            if (ShouldUseParallelExecute(message))
            {
                await WriteToChannelAsync(_receivedChannel, (message, descriptor)).ConfigureAwait(false);
            }
            else
            {
                await _executor.ExecuteAsync(message, descriptor, _tasksCts!.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An exception occurred when invoke subscriber. MessageId:{MessageId}", message.DbId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _tasksCts?.Dispose();
    }

    #endregion

    #region Initialization Methods

    private void ResetStateIfNeeded()
    {
        if (_disposed || (_tasksCts != null && _tasksCts.IsCancellationRequested))
        {
            _tasksCts?.Dispose();
            _tasksCts = null;
            _disposed = false;
        }
    }

    private void InitializePublishedChannel()
    {
        _publishedChannel = Channel.CreateBounded<MediumMessage>(new BoundedChannelOptions(_publishChannelSize)
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = !_enableParallelSend,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    private void InitializeReceivedChannel()
    {
        var bufferSize = _options.SubscriberParallelExecuteThreadCount * _options.SubscriberParallelExecuteBufferFactor;
        var isSingleReader = _options.SubscriberParallelExecuteThreadCount == 1;

        _receivedChannel = Channel.CreateBounded<(MediumMessage, ConsumerExecutorDescriptor?)>(
        new BoundedChannelOptions(bufferSize)
        {
            AllowSynchronousContinuations = true,
            SingleReader = isSingleReader,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    #endregion

    #region Task Startup Methods

    private async Task StartSendingTaskAsync()
    {
        await Task.Run(SendingAsync, _tasksCts!.Token).ConfigureAwait(false);
    }

    private async Task StartProcessingTasksAsync()
    {
        var processingTasks = Enumerable
            .Range(0, _options.SubscriberParallelExecuteThreadCount)
            .Select(_ => Task.Run(ProcessingAsync, _tasksCts!.Token))
            .ToArray();

        await Task.WhenAll(processingTasks).ConfigureAwait(false);
    }

    private Task StartSchedulerTaskAsync()
    {
        return Task.Run(async () =>
        {
            RegisterSchedulerCancellationHandler();

            while (!_tasksCts!.Token.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledMessagesAsync().ConfigureAwait(false);
                    _tasksCts.Token.WaitHandle.WaitOne(100);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Delay message publishing failed unexpectedly, which will stop future scheduled " +
                        "messages from publishing. See more details here: https://github.com/dotnetcore/CAP/issues/1637. " +
                        "Exception: {Message}",
                        ex.Message);
                    throw;
                }
            }
        }, _tasksCts!.Token);
    }

    #endregion

    #region Scheduler Methods

    private void RegisterSchedulerCancellationHandler()
    {
        _tasksCts!.Token.Register(() =>
        {
            try
            {
                if (_schedulerQueue.Count == 0)
                {
                    return;
                }

                var messageIds = _schedulerQueue.UnorderedItems.Select(x => x.DbId).ToArray();
                _storage.ChangePublishStateToDelayedAsync(messageIds)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                _logger.LogDebug("Update storage to delayed success of delayed message in memory queue!");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Update storage fails of delayed message in memory queue!");
            }
        });
    }

    private async Task ProcessScheduledMessagesAsync()
    {
        await foreach (var nextMessage in _schedulerQueue.GetConsumingEnumerable(_tasksCts!.Token))
        {
            _tasksCts.Token.ThrowIfCancellationRequested();

            if (ShouldUseParallelSend(nextMessage))
            {
                await WriteToChannelAsync(_publishedChannel, nextMessage).ConfigureAwait(false);
            }
            else
            {
                await SendScheduledMessageDirectlyAsync(nextMessage).ConfigureAwait(false);
            }
        }
    }

    private async Task SendScheduledMessageDirectlyAsync(MediumMessage message)
    {
        try
        {
            var result = await _sender.SendAsync(message).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                _logger.LogError("Delay message sending failed. MessageId: {MessageId} ", message.DbId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending scheduled message. MessageId: {MessageId}", message.DbId);
        }
    }

    #endregion

    #region Background Workers - Sending

    private async ValueTask SendingAsync()
    {
        try
        {
            while (await _publishedChannel.Reader.WaitToReadAsync(_tasksCts!.Token).ConfigureAwait(false))
            {
                if (_enableParallelSend)
                {
                    await SendBatchParallelAsync().ConfigureAwait(false);
                }
                else
                {
                    await SendBatchSequentialAsync().ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    private async Task SendBatchParallelAsync()
    {
        var batchSize = Math.Max(1, _publishChannelSize / 50);
        var tasks = new List<Task>(batchSize);

        for (var i = 0; i < batchSize && _publishedChannel.Reader.TryRead(out var message); i++)
        {
            tasks.Add(SendMessageAsync(message));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private async Task SendBatchSequentialAsync()
    {
        while (_publishedChannel.Reader.TryRead(out var message))
        {
            await SendMessageAsync(message).ConfigureAwait(false);
        }
    }

    private async Task SendMessageAsync(MediumMessage message)
    {
        try
        {
            var result = await _sender.SendAsync(message).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                _logger.MessagePublishException(message.Origin.GetId(), result.ToString(), result.Exception);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred when sending a message to the transport. Id:{MessageId}", message.DbId);
        }
    }

    private async Task SendMessageDirectlyAsync(MediumMessage message)
    {
        var result = await _sender.SendAsync(message).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            _logger.MessagePublishException(message.Origin.GetId(), result.ToString(), result.Exception);
        }
    }

    #endregion

    #region Background Workers - Processing

    private async ValueTask ProcessingAsync()
    {
        try
        {
            while (await _receivedChannel.Reader.WaitToReadAsync(_tasksCts!.Token).ConfigureAwait(false))
            {
                while (_receivedChannel.Reader.TryRead(out var messageData))
                {
                    await ProcessReceivedMessageAsync(messageData).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    private async Task ProcessReceivedMessageAsync((MediumMessage, ConsumerExecutorDescriptor?) messageData)
    {
        try
        {
            var (message, descriptor) = messageData;
            await _executor.ExecuteAsync(message, descriptor, _tasksCts!.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An exception occurred when invoke subscriber. MessageId:{MessageId}", messageData.Item1.DbId);
        }
    }

    #endregion

    #region Helper Methods

    private bool IsCancellationRequested()
    {
        return _tasksCts?.IsCancellationRequested ?? true;
    }

    private bool ShouldUseParallelSend(MediumMessage message)
    {
        return _enableParallelSend && message.Retries == 0;
    }

    private bool ShouldUseParallelExecute(MediumMessage message)
    {
        return _enableParallelExecute && message.Retries == 0;
    }

    private async ValueTask WriteToChannelAsync<T>(Channel<T> channel, T item)
    {
        if (!channel.Writer.TryWrite(item))
        {
            while (await channel.Writer.WaitToWriteAsync(_tasksCts!.Token).ConfigureAwait(false))
            {
                if (channel.Writer.TryWrite(item))
                {
                    break;
                }
            }
        }
    }

    #endregion
}
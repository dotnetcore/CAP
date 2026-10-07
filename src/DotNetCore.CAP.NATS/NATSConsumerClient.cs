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
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace DotNetCore.CAP.NATS;

internal sealed class NATSConsumerClient : IConsumerClient
{
    private static readonly SemaphoreSlim StreamLock = new(1, 1);
    private readonly string _groupName;
    private readonly IServiceProvider _serviceProvider;
    private readonly NATSOptions _options;
    private readonly SemaphoreSlim? _semaphore;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly CancellationTokenSource _stopping = new();
    private readonly TaskCompletionSource<string> _disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<Task> _consumers = new();
    private NatsConnection? _connection;
    private int _disposed;

    public NATSConsumerClient(string groupName, byte groupConcurrent, IOptions<NATSOptions> options, IServiceProvider serviceProvider)
    {
        _groupName = groupName;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        if (groupConcurrent > 0)
            _semaphore = new SemaphoreSlim(groupConcurrent, groupConcurrent);
    }

    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }
    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }
    public BrokerAddress BrokerAddress => new("nats", _options.Servers);

    public async Task ConnectAsync()
    {
        await _connectLock.WaitAsync(_stopping.Token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            if (_connection != null) return;
            var opts = (_options.Options ?? NatsOpts.Default) with
            {
                Url = _options.Servers,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                MaxReconnectRetry = 0,
                Echo = false,
                SubPendingChannelCapacity = _options.Options?.SubPendingChannelCapacity ?? 1_000,
                SubPendingChannelFullMode = BoundedChannelFullMode.Wait,
                PublishTimeoutOnDisconnected = true,
                LoggerFactory = _options.Options?.LoggerFactory ??
                    _serviceProvider.GetService<ILoggerFactory>() ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
            };
            var connection = new NatsConnection(opts);
            connection.ConnectionDisconnected += OnDisconnected;
            connection.ServerError += OnServerError;
            connection.MessageDropped += OnMessageDropped;
            try
            {
                await connection.ConnectAsync().AsTask().WaitAsync(_stopping.Token).ConfigureAwait(false);
                _connection = connection;
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task<ICollection<string>> FetchTopicsAsync(IEnumerable<string> topicNames)
    {
        var topics = topicNames.Distinct().ToList();
        if (!_options.EnableSubscriberClientStreamAndSubjectCreation) return topics;
        try
        {
            await ConnectAsync().ConfigureAwait(false);
            var js = new NatsJSContext(_connection!);
            await StreamLock.WaitAsync(_stopping.Token).ConfigureAwait(false);
            try
            {
                foreach (var group in topics.GroupBy(_options.NormalizeStreamName))
                {
                    INatsJSStream? stream = null;
                    try
                    {
                        stream = await js.GetStreamAsync(group.Key, cancellationToken: _stopping.Token).ConfigureAwait(false);
                    }
                    catch (NatsJSApiException ex) when (ex.Error.ErrCode == 10059) // Stream not found.
                    {
                    }

                    if (stream == null)
                    {
                        var config = new StreamConfig(group.Key, group.ToList())
                        {
                            NoAck = false,
                            Storage = StreamConfigStorage.Memory
                        };
                        _options.StreamOptions?.Invoke(config);
                        try
                        {
                            await js.CreateStreamAsync(config, _stopping.Token).ConfigureAwait(false);
                            continue;
                        }
                        catch (NatsJSApiException ex) when (ex.Error.ErrCode == 10058) // Another instance created it.
                        {
                            stream = await js.GetStreamAsync(group.Key, cancellationToken: _stopping.Token).ConfigureAwait(false);
                        }
                    }

                    // Preserve server-side settings and subjects used by other CAP groups.
                    var updated = stream.Info.Config with
                    {
                        Subjects = (stream.Info.Config.Subjects ?? Array.Empty<string>()).Union(group).ToList()
                    };
                    _options.StreamOptions?.Invoke(updated);
                    updated.Subjects = (updated.Subjects ?? Array.Empty<string>()).Union(group).ToList();
                    await js.UpdateStreamAsync(updated, _stopping.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                StreamLock.Release();
            }
            return topics;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new BrokerConnectionException(ex);
        }
    }

    public async Task SubscribeAsync(IEnumerable<string> topics)
    {
        ArgumentNullException.ThrowIfNull(topics);
        try
        {
            await ConnectAsync().ConfigureAwait(false);
            var js = new NatsJSContext(_connection!);
            foreach (var subject in topics.Distinct())
            {
                var stream = _options.NormalizeStreamName(subject);
                var group = Helper.Normalized(_groupName);
                var opts = new NatsJSPushConsumerOpts
                {
                    DurableName = Helper.Normalized(group + "-" + subject),
                    DeliverGroup = group,
                    FilterSubject = subject,
                    DeliverSubject = js.NewBaseInbox(),
                    DeliverPolicy = ConsumerConfigDeliverPolicy.New,
                    AckPolicy = ConsumerConfigAckPolicy.Explicit,
                    AckWait = TimeSpan.FromSeconds(30),
                    // The SDK otherwise hardcodes a capacity of 1000 for newly created consumers.
                    SubOpts = new NatsSubOpts
                    {
                        ChannelOpts = new NatsSubChannelOpts
                        {
                            Capacity = _connection!.Opts.SubPendingChannelCapacity,
                            FullMode = _connection.Opts.SubPendingChannelFullMode
                        }
                    }
                };
                opts = _options.ConsumerOptions?.Invoke(opts) ?? opts;
                if (string.IsNullOrWhiteSpace(opts.DurableName) || opts.DeliverGroup != group ||
                    opts.FilterSubject != subject || opts.FilterSubjects is { Count: > 0 } ||
                    opts.AckPolicy != ConsumerConfigAckPolicy.Explicit)
                    throw new InvalidOperationException("CAP requires a durable push consumer with its topic filter, queue group and explicit acknowledgments.");

                INatsJSPushConsumer consumer;
                try
                {
                    consumer = await js.GetPushConsumerAsync(stream, opts.DurableName, _stopping.Token).ConfigureAwait(false);
                }
                catch (NatsJSApiException ex) when (ex.Error.ErrCode == 10014) // Consumer not found.
                {
                    try
                    {
                        consumer = await js.CreatePushConsumerAsync(stream, opts, _stopping.Token).ConfigureAwait(false);
                    }
                    catch (NatsJSApiException conflict) when (conflict.Error.ErrCode is 10013 or 10148)
                    {
                        consumer = await js.GetPushConsumerAsync(stream, opts.DurableName, _stopping.Token).ConfigureAwait(false);
                    }
                }

                var config = consumer.Info.Config;
                if (config.DeliverGroup != group || config.FilterSubject != subject ||
                    config.FilterSubjects is { Count: > 0 } || config.AckPolicy != ConsumerConfigAckPolicy.Explicit)
                    throw new InvalidOperationException($"Existing NATS consumer '{stream}/{opts.DurableName}' is incompatible with CAP's topic, queue group or acknowledgment policy.");

                _consumers.Add(ConsumeAsync(consumer, opts.NotificationHandler));
            }
            // Flush subscriptions before publishers on another connection start sending.
            await _connection!.PingAsync(_stopping.Token).ConfigureAwait(false);
            foreach (var task in _consumers.Where(task => task.IsCompleted))
            {
                await task.ConfigureAwait(false);
                throw new NatsJSException("NATS push consumer stopped during subscription.");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _stopping.Cancel();
            OnLogCallback?.Invoke(new LogMessageEventArgs { LogType = MqLogType.ConnectError, Reason = ex.ToString() });
            throw new BrokerConnectionException(ex);
        }
    }

    public async Task ListeningAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.Register(() => _stopping.Cancel());
        var completed = await Task.WhenAny(_consumers.Append(_disconnected.Task)
            .Append(Task.Delay(Timeout.InfiniteTimeSpan, _stopping.Token))).ConfigureAwait(false);
        _stopping.Token.ThrowIfCancellationRequested();
        try
        {
            await completed.ConfigureAwait(false);
            if (completed == _disconnected.Task)
                throw new NatsJSException(await _disconnected.Task.ConfigureAwait(false));
            throw new NatsJSException("NATS push consumer stopped unexpectedly.");
        }
        catch (Exception ex)
        {
            OnLogCallback?.Invoke(new LogMessageEventArgs { LogType = MqLogType.ConnectError, Reason = ex.ToString() });
            throw new BrokerConnectionException(ex);
        }
        finally
        {
            _stopping.Cancel();
        }
    }

    private async Task ConsumeAsync(INatsJSPushConsumer consumer,
        Func<INatsJSNotification, CancellationToken, Task>? notificationHandler)
    {
        var pending = new HashSet<Task>();
        try
        {
            await foreach (var msg in consumer.ConsumeAsync<byte[]>(serializer: NatsRawSerializer<byte[]>.Default,
                               opts: new NatsJSConsumeOpts { NotificationHandler = notificationHandler },
                               cancellationToken: _stopping.Token).ConfigureAwait(false))
            {
                if (_semaphore == null)
                {
                    await ProcessAsync(msg).ConfigureAwait(false);
                    continue;
                }
                await _semaphore.WaitAsync(_stopping.Token).ConfigureAwait(false);
                foreach (var task in pending.Where(task => task.IsCompleted).ToArray())
                {
                    await task.ConfigureAwait(false);
                    pending.Remove(task);
                }
                pending.Add(ProcessAsync(msg));
            }
        }
        finally
        {
            await Task.WhenAll(pending).ConfigureAwait(false);
        }
    }

    private async Task ProcessAsync(INatsJSMsg<byte[]> msg)
    {
        try
        {
            msg.EnsureSuccess();
            var headers = new Dictionary<string, string?>();
            if (msg.Headers != null)
                foreach (var header in msg.Headers)
                    headers[header.Key] = header.Value.ToString();
            headers[Headers.Group] = _groupName;
            if (_options.CustomHeadersBuilder != null)
                foreach (var header in _options.CustomHeadersBuilder(msg, _serviceProvider))
                    headers[header.Key] = header.Value;
            await OnMessageCallback!(new TransportMessage(headers, msg.Data ?? Array.Empty<byte>()), msg).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnLogCallback?.Invoke(new LogMessageEventArgs { LogType = MqLogType.AsyncErrorEvent, Reason = ex.ToString() });
            await RejectAsync(msg).ConfigureAwait(false);
        }
        finally
        {
            _semaphore?.Release();
        }
    }

    public async Task CommitAsync(object? sender)
    {
        try
        {
            if (sender is INatsJSMsg<byte[]> msg)
                await msg.AckAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnLogCallback?.Invoke(new LogMessageEventArgs { LogType = MqLogType.AsyncErrorEvent, Reason = $"NATS message ACK failed: {ex}" });
        }
    }

    public async Task RejectAsync(object? sender)
    {
        try
        {
            if (sender is INatsJSMsg<byte[]> msg)
                await msg.NakAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnLogCallback?.Invoke(new LogMessageEventArgs { LogType = MqLogType.AsyncErrorEvent, Reason = $"NATS message NAK failed: {ex}" });
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stopping.Cancel();
        try
        {
            await Task.WhenAll(_consumers).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
                OnLogCallback?.Invoke(new LogMessageEventArgs { LogType = MqLogType.AsyncErrorEvent, Reason = ex.ToString() });
        }
        await _connectLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_connection != null)
                await _connection.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _connectLock.Release();
            _semaphore?.Dispose();
            _stopping.Dispose();
        }
    }

    private ValueTask OnDisconnected(object? sender, NatsEventArgs args)
    {
        if (!_stopping.IsCancellationRequested) _disconnected.TrySetResult(args.Message);
        return default;
    }

    private ValueTask OnServerError(object? sender, NatsServerErrorEventArgs args)
    {
        OnLogCallback?.Invoke(new LogMessageEventArgs { LogType = MqLogType.AsyncErrorEvent, Reason = args.Error });
        if (!_stopping.IsCancellationRequested) _disconnected.TrySetResult(args.Message);
        return default;
    }

    private ValueTask OnMessageDropped(object? sender, NatsMessageDroppedEventArgs args)
    {
        OnLogCallback?.Invoke(new LogMessageEventArgs { LogType = MqLogType.AsyncErrorEvent, Reason = args.Message });
        return default;
    }
}

// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotNetCore.CAP.RabbitMQ;

public class RabbitMqBasicConsumer : AsyncDefaultBasicConsumer
{
    private readonly SemaphoreSlim _semaphore;
    private readonly string _groupName;
    private readonly bool _usingTaskRun;
    private readonly Func<TransportMessage, object?, Task> _msgCallback;
    private readonly Action<LogMessageEventArgs> _logCallback;
    private readonly Func<BasicDeliverEventArgs, IServiceProvider, List<KeyValuePair<string, string>>>? _customHeadersBuilder;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMqBasicConsumer(IChannel channel, 
        byte concurrent,
        string groupName,
        Func<TransportMessage, object?, Task> msgCallback,
        Action<LogMessageEventArgs> logCallback,
        Func<BasicDeliverEventArgs, IServiceProvider, List<KeyValuePair<string, string>>>? customHeadersBuilder,
        IServiceProvider serviceProvider
        ) : base(channel)
    {
        _semaphore = new SemaphoreSlim(concurrent);
        _groupName = groupName;
        _usingTaskRun = concurrent > 0;
        _msgCallback = msgCallback;
        _logCallback = logCallback;
        _customHeadersBuilder = customHeadersBuilder;
        _serviceProvider = serviceProvider;
    }

    public override async Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange,
        string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        var safeBody = _usingTaskRun ? body.ToArray() : body;

        if (_usingTaskRun)
        {
            await _semaphore.WaitAsync(cancellationToken);

            _ = Task.Run(Consume, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await Consume().ConfigureAwait(false);
        }

        Task Consume()
        {
            var headers = new Dictionary<string, string?>();

            if (properties.Headers != null)
                foreach (var header in properties.Headers)
                {
                    if (header.Value is byte[] val)
                        headers.Add(header.Key, Encoding.UTF8.GetString(val));
                    else
                        headers.Add(header.Key, header.Value?.ToString());
                }

            headers[Messages.Headers.Group] = _groupName;

            if (_customHeadersBuilder != null)
            {
                var e = new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey,
                    properties, safeBody);
                var customHeaders = _customHeadersBuilder(e, _serviceProvider);
                foreach (var customHeader in customHeaders)
                {
                    headers[customHeader.Key] = customHeader.Value;
                }
            }

            var message = new TransportMessage(headers, safeBody);

            return _msgCallback(message, deliveryTag);
        }
    }

    public async Task BasicAck(ulong deliveryTag)
    {
        if (Channel.IsOpen)
           await Channel.BasicAckAsync(deliveryTag, false);

        _semaphore.Release();
    }

    public async Task BasicReject(ulong deliveryTag)
    {
        if (Channel.IsOpen)
           await Channel.BasicRejectAsync(deliveryTag, true);

        _semaphore.Release();
    }


    protected override async Task OnCancelAsync(string[] consumerTags, CancellationToken cancellationToken = default)
    {
        await base.OnCancelAsync(consumerTags, cancellationToken);

        var args = new LogMessageEventArgs
        {
            LogType = MqLogType.ConsumerCancelled,
            Reason = string.Join(",", consumerTags)
        };

        _logCallback(args);
    }

    public override async Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        await base.HandleBasicCancelOkAsync(consumerTag, cancellationToken);

        var args = new LogMessageEventArgs
        {
            LogType = MqLogType.ConsumerUnregistered,
            Reason = consumerTag
        };

        _logCallback(args);
    }

    public override async Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        await base.HandleBasicConsumeOkAsync(consumerTag, cancellationToken);

        var args = new LogMessageEventArgs
        {
            LogType = MqLogType.ConsumerRegistered,
            Reason = consumerTag
        };

        _logCallback(args);
    }

    public override async Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason)
    {
        await base.HandleChannelShutdownAsync(channel, reason);

        var args = new LogMessageEventArgs
        {
            LogType = MqLogType.ConsumerShutdown,
            Reason = reason.ReplyText
        };

        _logCallback(args);
    }
}

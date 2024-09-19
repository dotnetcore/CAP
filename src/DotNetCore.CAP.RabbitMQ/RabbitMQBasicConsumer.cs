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

public class RabbitMQBasicConsumer : AsyncDefaultBasicConsumer
{
    private readonly SemaphoreSlim _semaphore;
    private readonly string _groupName;
    private readonly bool _usingTaskRun;
    private readonly Func<TransportMessage, object?, Task> _msgCallback;
    private readonly Action<LogMessageEventArgs> _logCallback;
    private readonly Func<BasicDeliverEventArgs, IServiceProvider, List<KeyValuePair<string, string>>>? _customHeadersBuilder;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQBasicConsumer(IModel? model,
        byte concurrent, string groupName,
        Func<TransportMessage, object?, Task> msgCallback,
        Action<LogMessageEventArgs> logCallback,
        Func<BasicDeliverEventArgs, IServiceProvider, List<KeyValuePair<string, string>>>? customHeadersBuilder,
        IServiceProvider serviceProvider)
            : base(model)
    {
        _semaphore = new SemaphoreSlim(concurrent);
        _groupName = groupName;
        _usingTaskRun = concurrent > 0;
        _msgCallback = msgCallback;
        _logCallback = logCallback;
        _customHeadersBuilder = customHeadersBuilder;
        _serviceProvider = serviceProvider;
    }

    public override async Task HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange,
        string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
    {
        if (_usingTaskRun)
        {
            await _semaphore.WaitAsync();

            _ = Task.Run(Consume).ConfigureAwait(false);
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

            headers.Add(Messages.Headers.Group, _groupName);

            if (_customHeadersBuilder != null)
            {
                var e = new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
                var customHeaders = _customHeadersBuilder(e, _serviceProvider);
                foreach (var customHeader in customHeaders)
                {
                    headers[customHeader.Key] = customHeader.Value;
                }
            }

            var message = new TransportMessage(headers, body);

            return _msgCallback(message, deliveryTag);
        }
    }

    public void BasicAck(ulong deliveryTag)
    {
        if (Model.IsOpen)
            Model.BasicAck(deliveryTag, false);

        _semaphore.Release();
    }

    public void BasicReject(ulong deliveryTag)
    {
        if (Model.IsOpen)
            Model.BasicReject(deliveryTag, true);

        _semaphore.Release();
    }

    public override async Task OnCancel(params string[] consumerTags)
    {
        await base.OnCancel(consumerTags);

        var args = new LogMessageEventArgs
        {
            LogType = MqLogType.ConsumerCancelled,
            Reason = string.Join(",", consumerTags)
        };

        _logCallback(args);
    }

    public override async Task HandleBasicCancelOk(string consumerTag)
    {
        await base.HandleBasicCancelOk(consumerTag);

        var args = new LogMessageEventArgs
        {
            LogType = MqLogType.ConsumerUnregistered,
            Reason = consumerTag
        };

        _logCallback(args);
    }

    public override async Task HandleBasicConsumeOk(string consumerTag)
    {
        await base.HandleBasicConsumeOk(consumerTag);

        var args = new LogMessageEventArgs
        {
            LogType = MqLogType.ConsumerRegistered,
            Reason = consumerTag
        };

        _logCallback(args);
    }

    public override async Task HandleModelShutdown(object model, ShutdownEventArgs reason)
    {
        await base.HandleModelShutdown(model, reason);

        var args = new LogMessageEventArgs
        {
            LogType = MqLogType.ConsumerShutdown,
            Reason = reason.ReplyText
        };

        _logCallback(args);
    }
}

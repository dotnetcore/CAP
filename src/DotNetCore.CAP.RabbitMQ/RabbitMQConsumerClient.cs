// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ;

internal sealed class RabbitMQConsumerClient : IConsumerClient
{
    private static readonly object Lock = new();
    private readonly IConnectionChannelPool _connectionChannelPool;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly byte _groupConcurrent;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private RabbitMQBasicConsumer? _consumer = null;
    private IModel? _channel;

    public RabbitMQConsumerClient(string groupName, byte groupConcurrent,
        IConnectionChannelPool connectionChannelPool,
        IOptions<RabbitMQOptions> options,
        IServiceProvider serviceProvider)
    {
        _queueName = groupName;
        _groupConcurrent = groupConcurrent;
        _connectionChannelPool = connectionChannelPool;
        _serviceProvider = serviceProvider;
        _rabbitMQOptions = options.Value;
        _exchangeName = connectionChannelPool.Exchange;
    }

    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

    public BrokerAddress BrokerAddress => new("RabbitMQ", $"{_rabbitMQOptions.HostName}:{_rabbitMQOptions.Port}");

    public void Subscribe(IEnumerable<string> topics)
    {
        if (topics == null) throw new ArgumentNullException(nameof(topics));

        Connect();

        foreach (var topic in topics)
        {
            _channel.QueueBind(_queueName, _exchangeName, topic);
        }
    }

    public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
    {
        Connect();

        if (_groupConcurrent > 0)
        {
            _channel?.BasicQos(prefetchSize: 0, prefetchCount: _groupConcurrent, global: false);
        }
        else if (_rabbitMQOptions.BasicQosOptions != null)
        {
            _channel?.BasicQos(0, _rabbitMQOptions.BasicQosOptions.PrefetchCount, _rabbitMQOptions.BasicQosOptions.Global);
        }

        _consumer = new RabbitMQBasicConsumer(_channel, _groupConcurrent, _queueName, OnMessageCallback!, OnLogCallback!,
            _rabbitMQOptions.CustomHeadersBuilder, _serviceProvider);

        try
        {
            _channel.BasicConsume(_queueName, false, _consumer);
        }
        catch (TimeoutException ex)
        {
            _consumer.HandleModelShutdown(null!, new ShutdownEventArgs(ShutdownInitiator.Application, 0,
                ex.Message + "-->" + nameof(_channel.BasicConsume))).GetAwaiter().GetResult();
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.WaitHandle.WaitOne(timeout);
        }

        // ReSharper disable once FunctionNeverReturns
    }

    public void Commit(object? sender)
    {
        _consumer!.BasicAck((ulong)sender!);
    }

    public void Reject(object? sender)
    {
        _consumer!.BasicAck((ulong)sender!);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        //The connection should not be closed here, because the connection is still in use elsewhere. 
        //_connection?.Dispose();
    }

    public void Connect()
    {
        var connection = _connectionChannelPool.GetConnection();

        lock (Lock)
        {
            if (_channel == null || _channel.IsClosed)
            {
                _channel = connection.CreateModel();

                _channel.ExchangeDeclare(_exchangeName, RabbitMQOptions.ExchangeType, true);

                var arguments = new Dictionary<string, object>
                {
                    { "x-message-ttl", _rabbitMQOptions.QueueArguments.MessageTTL }
                };

                if (!string.IsNullOrEmpty(_rabbitMQOptions.QueueArguments.QueueMode))
                    arguments.Add("x-queue-mode", _rabbitMQOptions.QueueArguments.QueueMode);

                if (!string.IsNullOrEmpty(_rabbitMQOptions.QueueArguments.QueueType))
                    arguments.Add("x-queue-type", _rabbitMQOptions.QueueArguments.QueueType);

                try
                {
                    _channel.QueueDeclare(_queueName, _rabbitMQOptions.QueueOptions.Durable, _rabbitMQOptions.QueueOptions.Exclusive, _rabbitMQOptions.QueueOptions.AutoDelete, arguments);
                }
                catch (TimeoutException ex)
                {
                    var args = new LogMessageEventArgs
                    {
                        LogType = MqLogType.ConsumerShutdown,
                        Reason = ex.Message + "-->" + nameof(_channel.QueueDeclare)
                    };

                    OnLogCallback!(args);
                }
            }
        }
    }
}
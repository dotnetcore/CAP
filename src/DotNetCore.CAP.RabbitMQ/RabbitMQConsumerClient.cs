// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Headers = DotNetCore.CAP.Messages.Headers;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClient : IConsumerClient
        {
            private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
    
            private readonly IConnectionChannelPool _connectionChannelPool;
            private readonly string _exchangeName;
            private readonly string _queueName;
            private readonly RabbitMQOptions _rabbitMQOptions;
            private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(ITransport));
            private static readonly TextMapPropagator Propagator = new TraceContextPropagator();
            private IModel _channel;
    
            private IConnection _connection;
    
            public RabbitMQConsumerClient(string queueName,
                IConnectionChannelPool connectionChannelPool,
                IOptions<RabbitMQOptions> options)
            {
                _queueName = queueName;
                _connectionChannelPool = connectionChannelPool;
                _rabbitMQOptions = options.Value;
                _exchangeName = connectionChannelPool.Exchange;
            }
    
            public event EventHandler<TransportMessage> OnMessageReceived;
    
            public event EventHandler<LogMessageEventArgs> OnLog;
    
            public BrokerAddress BrokerAddress => new BrokerAddress("RabbitMQ", _rabbitMQOptions.HostName);
    
            public void Subscribe(IEnumerable<string> topics)
            {
                if (topics == null)
                {
                    throw new ArgumentNullException(nameof(topics));
                }
    
                Connect();
    
                foreach (var topic in topics)
                {
                    _channel.QueueBind(_queueName, _exchangeName, topic);
                }
            }
    
            public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
            {
                Connect();
    
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += OnConsumerReceived;
                consumer.Shutdown += OnConsumerShutdown;
                consumer.Registered += OnConsumerRegistered;
                consumer.Unregistered += OnConsumerUnregistered;
                consumer.ConsumerCancelled += OnConsumerConsumerCancelled;
    
                _channel.BasicConsume(_queueName, false, consumer);
    
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    cancellationToken.WaitHandle.WaitOne(timeout);
                }
    
                // ReSharper disable once FunctionNeverReturns
            }
    
            public void Commit(object sender)
            {
                if (_channel.IsOpen)
                {
                    _channel.BasicAck((ulong)sender, false);
                }
            }
    
            public void Reject(object sender)
            {
                if (_channel.IsOpen)
                {
                    _channel.BasicReject((ulong)sender, true);
                }
            }
    
            public void Dispose()
            {
    
                _channel?.Dispose();
                //The connection should not be closed here, because the connection is still in use elsewhere. 
                //_connection?.Dispose();
            }
    
            public void Connect()
            {
                if (_connection != null)
                {
                    return;
                }
    
                _connectionLock.Wait();
    
                try
                {
                    if (_connection == null)
                    {
                        _connection = _connectionChannelPool.GetConnection();
    
                        _channel = _connection.CreateModel();
    
                        _channel.ExchangeDeclare(_exchangeName, RabbitMQOptions.ExchangeType, true);
    
                        var arguments = new Dictionary<string, object>
                        {
                            {"x-message-ttl", _rabbitMQOptions.QueueArguments.MessageTTL}
                        };
    
                        if (!string.IsNullOrEmpty(_rabbitMQOptions.QueueArguments.QueueMode))
                        {
                            arguments.Add("x-queue-mode", _rabbitMQOptions.QueueArguments.QueueMode);
                        }
    
                        _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
                    }
                }
                finally
                {
                    _connectionLock.Release();
                }
            }
    
            #region events
    
            private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)
            {
                var args = new LogMessageEventArgs
                {
                    LogType = MqLogType.ConsumerCancelled,
                    Reason = string.Join(",", e.ConsumerTags)
                };
                OnLog?.Invoke(sender, args);
            }
    
            private void OnConsumerUnregistered(object sender, ConsumerEventArgs e)
            {
                var args = new LogMessageEventArgs
                {
                    LogType = MqLogType.ConsumerUnregistered,
                    Reason = string.Join(",", e.ConsumerTags)
                };
                OnLog?.Invoke(sender, args);
            }
    
            private void OnConsumerRegistered(object sender, ConsumerEventArgs e)
            {
                var args = new LogMessageEventArgs
                {
                    LogType = MqLogType.ConsumerRegistered,
                    Reason = string.Join(",", e.ConsumerTags)
                };
                OnLog?.Invoke(sender, args);
            }
    
            private void OnConsumerReceived(object sender, BasicDeliverEventArgs e)
            {
                var parentContext = Propagator.Extract(default, e.BasicProperties, ExtractTraceContextFromBasicProperties);
                Baggage.Current = parentContext.Baggage;
                
                var activityName = e.RoutingKey;
    
                using (var activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext))
                {
                    AddMessagingTags(activity, e);
                    
                    var headers = new Dictionary<string, string>();
                    if (e.BasicProperties.Headers != null)
                    {
                        foreach (var header in e.BasicProperties.Headers)
                        {
                            headers.Add(header.Key,
                                header.Value == null ? null : Encoding.UTF8.GetString((byte[]) header.Value));
                        }
                    }
    
                    headers.Add(Headers.Group, _queueName);
    
                    if (_rabbitMQOptions.CustomHeaders != null)
                    {
                        var customHeaders = _rabbitMQOptions.CustomHeaders(e);
                        foreach (var customHeader in customHeaders)
                        {
                            headers[customHeader.Key] = customHeader.Value;
                        }
                    }
    
                    var message = new TransportMessage(headers, e.Body.ToArray());
    
                    OnMessageReceived?.Invoke(e.DeliveryTag, message);
                }
            }
    
            private void OnConsumerShutdown(object sender, ShutdownEventArgs e)
            {
                var args = new LogMessageEventArgs
                {
                    LogType = MqLogType.ConsumerShutdown,
                    Reason = e.ReplyText
                };
    
                OnLog?.Invoke(sender, args);
            }
            
            private void AddMessagingTags(Activity activity, BasicDeliverEventArgs e)
            {
                activity?.SetTag("message",  Encoding.UTF8.GetString(e.Body.Span.ToArray()));
                activity?.SetTag("messaging_system", "rabbitmq");
                activity?.SetTag("destination_kind", "queue");
                activity?.SetTag("exchange_name", _exchangeName);
                activity?.SetTag("routing_key", e.RoutingKey);
            }
            
            private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
            {
                if (!props.Headers.TryGetValue(key, out var value)) return Enumerable.Empty<string>();
                var bytes = value as byte[];
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
    
            #endregion
        }
}
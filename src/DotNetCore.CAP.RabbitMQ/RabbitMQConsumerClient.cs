// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private IModel _channel;

        private IConnection _connection;
        private ulong _deliveryTag;

        public RabbitMQConsumerClient(string queueName,
            IConnectionChannelPool connectionChannelPool,
            IOptions<RabbitMQOptions> options)
        {
            _queueName = queueName;
            _connectionChannelPool = connectionChannelPool;
            _rabbitMQOptions = options.Value;
            _exchangeName = connectionChannelPool.Exchange;
        }

        public event EventHandler<MessageContext> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public string ServersAddress => _rabbitMQOptions.HostName;

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

        public void Commit()
        {
            _channel.BasicAck(_deliveryTag, false);
        }

        public void Reject()
        {
            _channel.BasicReject(_deliveryTag, true);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }

        #region events

        private void Connect()
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
                        {"x-message-ttl", _rabbitMQOptions.QueueMessageExpires}
                    };
                    _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        } 

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)
        {
            var args = new LogMessageEventArgs
            {
                LogType = MqLogType.ConsumerCancelled,
                Reason = e.ConsumerTag
            };
            OnLog?.Invoke(sender, args);
        }

        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e)
        {
            var args = new LogMessageEventArgs
            {
                LogType = MqLogType.ConsumerUnregistered,
                Reason = e.ConsumerTag
            };
            OnLog?.Invoke(sender, args);
        }

        private void OnConsumerRegistered(object sender, ConsumerEventArgs e)
        {
            var args = new LogMessageEventArgs
            {
                LogType = MqLogType.ConsumerRegistered,
                Reason = e.ConsumerTag
            };
            OnLog?.Invoke(sender, args);
        }

        private void OnConsumerReceived(object sender, BasicDeliverEventArgs e)
        {
            _deliveryTag = e.DeliveryTag;
            var message = new MessageContext
            {
                Group = _queueName,
                Name = e.RoutingKey,
                Content = Encoding.UTF8.GetString(e.Body)
            };
            OnMessageReceived?.Invoke(sender, message);
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

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClient : IConsumerClient
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly string _exchageName;
        private readonly string _queueName;
        private readonly RabbitMQOptions _rabbitMQOptions;

        private IConnection _connection;
        private IModel _channel;
        private ulong _deliveryTag;

        public RabbitMQConsumerClient(string queueName,
            IConnectionChannelPool connectionChannelPool,
            RabbitMQOptions options)
        {
            _queueName = queueName;
            _connectionChannelPool = connectionChannelPool;
            _rabbitMQOptions = options;
            _exchageName = options.TopicExchangeName;

            InitClient();
        }

        public event EventHandler<MessageContext> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null) throw new ArgumentNullException(nameof(topics));

            foreach (var topic in topics)
                _channel.QueueBind(_queueName, _exchageName, topic);
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
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
            _channel.Dispose();
            _connection.Dispose();
        }

        private void InitClient()
        {
            _connection = _connectionChannelPool.GetConnection();

            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                _exchageName,
                RabbitMQOptions.ExchangeType,
                true);

            var arguments = new Dictionary<string, object> {
                { "x-message-ttl", _rabbitMQOptions.QueueMessageExpires }
            };
            _channel.QueueDeclare(_queueName, true, false, false, arguments);
        }

        #region events

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)
        {
            var args = new LogMessageEventArgs
            {
                LogType =  MqLogType.ConsumerCancelled,
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
                Reason = e.ToString()
            };
            OnLog?.Invoke(sender, args);
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClient : IConsumerClient
    {
        private readonly string _exchageName;
        private readonly string _queueName;
        private readonly RabbitMQOptions _rabbitMQOptions;

        private ConnectionPool _connectionPool;
        private IModel _channel;
        private ulong _deliveryTag;

        public event EventHandler<MessageContext> OnMessageReceieved;

        public event EventHandler<string> OnError;

        public RabbitMQConsumerClient(string queueName,
             ConnectionPool connectionPool,
             RabbitMQOptions options)
        {
            _queueName = queueName;
            _connectionPool = connectionPool;
            _rabbitMQOptions = options;
            _exchageName = options.TopicExchangeName;

            InitClient();
        }

        private void InitClient()
        {
            var connection = _connectionPool.Rent();

            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: _exchageName,
                type: RabbitMQOptions.ExchangeType,
                durable: true);

            var arguments = new Dictionary<string, object> { { "x-message-ttl", (int)_rabbitMQOptions.QueueMessageExpires } };
            _channel.QueueDeclare(_queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            _connectionPool.Return(connection);
        }

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null) throw new ArgumentNullException(nameof(topics));

            foreach (var topic in topics)
            {
                _channel.QueueBind(_queueName, _exchageName, topic);
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnConsumerReceived;
            consumer.Shutdown += OnConsumerShutdown;
            _channel.BasicConsume(_queueName, false, consumer);
            while (true)
            {
                Task.Delay(timeout, cancellationToken).GetAwaiter().GetResult();
            }
        }

        public void Commit()
        {
            _channel.BasicAck(_deliveryTag, false);
        }

        public void Dispose()
        {
            _channel.Dispose();
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
            OnMessageReceieved?.Invoke(sender, message);
        }

        private void OnConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            OnError?.Invoke(sender, e.Cause?.ToString());
        }
    }
}
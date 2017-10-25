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
        private readonly ConnectionPool _connectionPool;
        private readonly string _exchageName;
        private readonly string _queueName;
        private readonly RabbitMQOptions _rabbitMQOptions;

        private IModel _channel;
        private ulong _deliveryTag;

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

        public event EventHandler<MessageContext> OnMessageReceived;

        public event EventHandler<string> OnError;

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
            _channel.BasicConsume(_queueName, false, consumer);
            while (true)
                Task.Delay(timeout, cancellationToken).GetAwaiter().GetResult();
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
        }

        private void InitClient()
        {
            var connection = _connectionPool.Rent();

            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(
                _exchageName,
                RabbitMQOptions.ExchangeType,
                true);

            var arguments = new Dictionary<string, object> {
                { "x-message-ttl", _rabbitMQOptions.QueueMessageExpires }
            };
            _channel.QueueDeclare(_queueName, true, false, false, arguments);

            _connectionPool.Return(connection);
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
            OnError?.Invoke(sender, e.Cause?.ToString());
        }
    }
}
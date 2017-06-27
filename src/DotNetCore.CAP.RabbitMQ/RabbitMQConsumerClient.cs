using System;
using System.Text;
using DotNetCore.CAP.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotNetCore.CAP.RabbitMQ
{
    public class RabbitMQConsumerClient : IConsumerClient
    {
        public const string TYPE = "topic";

        private string _queueName;
        private readonly string _exchange;
        private readonly RabbitMQOptions _rabbitMQOptions;

        private IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public event EventHandler<MessageBase> MessageReceieved;

        public RabbitMQConsumerClient(string exchange, RabbitMQOptions options)
        {
            _exchange = exchange;
            _rabbitMQOptions = options;

            InitClient();
        }

        private void InitClient()
        {
            _connectionFactory = new ConnectionFactory()
            {
                HostName = _rabbitMQOptions.HostName,
                UserName = _rabbitMQOptions.UserName,
                Port = _rabbitMQOptions.Port,
                Password = _rabbitMQOptions.Password,
                VirtualHost = _rabbitMQOptions.VirtualHost,
                RequestedConnectionTimeout = _rabbitMQOptions.RequestedConnectionTimeout,
                SocketReadTimeout = _rabbitMQOptions.SocketReadTimeout,
                SocketWriteTimeout = _rabbitMQOptions.SocketWriteTimeout
            };

            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: _exchange, type: TYPE);
            _queueName = _channel.QueueDeclare().QueueName;
        }

        public void Listening(TimeSpan timeout)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnConsumerReceived;
            _channel.BasicConsume(_queueName, true, consumer);
        }

        public void Subscribe(string topic)
        {
            _channel.QueueBind(_queueName, _exchange, topic);
        }

        public void Subscribe(string topic, int partition)
        {
            _channel.QueueBind(_queueName, _exchange, topic);
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
        }

        private void OnConsumerReceived(object sender, BasicDeliverEventArgs e)
        {
            var message = new MessageBase
            {
                KeyName = e.RoutingKey,
                Content = Encoding.UTF8.GetString(e.Body)
            };
            MessageReceieved?.Invoke(sender, message);
        }
    }
}
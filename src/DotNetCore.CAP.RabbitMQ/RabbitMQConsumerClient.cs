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

        private readonly string _exchange;
        private readonly string _hostName;

        private IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        private string _queueName;

        public event EventHandler<DeliverMessage> MessageReceieved;

        public RabbitMQConsumerClient(string exchange, string hostName)
        {
            _exchange = exchange;
            _hostName = hostName;

            InitClient();
        }

        private void InitClient()
        {
            _connectionFactory = new ConnectionFactory { HostName = _hostName };
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: _exchange, type: TYPE);
            _queueName = _channel.QueueDeclare().QueueName;
        }

        public void Listening(TimeSpan timeout)
        {
            //   Task.Delay(timeout).Wait();

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
            var message = new DeliverMessage
            {
                MessageKey = e.RoutingKey,
                Body = e.Body,
                Value = Encoding.UTF8.GetString(e.Body)
            };
            MessageReceieved?.Invoke(sender, message);
        }
    }
}
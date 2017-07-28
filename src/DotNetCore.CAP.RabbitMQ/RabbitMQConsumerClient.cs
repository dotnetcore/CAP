using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DotNetCore.CAP.RabbitMQ
{
    public class RabbitMQConsumerClient : IConsumerClient
    {
        private readonly string _exchageName;
        private readonly string _queueName;
        private readonly RabbitMQOptions _rabbitMQOptions;

        private IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private ulong _deliveryTag;

        public event EventHandler<MessageContext> OnMessageReceieved;

        public event EventHandler<string> OnError;

        public RabbitMQConsumerClient(string queueName, RabbitMQOptions options)
        {
            _queueName = queueName;
            _rabbitMQOptions = options;
            _exchageName = options.TopicExchangeName;

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
            _channel.ExchangeDeclare(exchange: _exchageName, type: RabbitMQOptions.ExchangeType);
            _channel.QueueDeclare(_queueName, exclusive: false);
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnConsumerReceived;
            consumer.Shutdown += OnConsumerShutdown;
            _channel.BasicConsume(_queueName, false, consumer);
            while (true)
            {
                Task.Delay(timeout, cancellationToken).Wait();
            }
        }

        public void Subscribe(string topic)
        {
            _channel.QueueBind(_queueName, _exchageName, topic);
        }

        public void Subscribe(string topic, int partition)
        {
            _channel.QueueBind(_queueName, _exchageName, topic);
        }

        public void Commit()
        {
            _channel.BasicAck(_deliveryTag, false);
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
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
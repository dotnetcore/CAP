using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IConnection _connection;


        public RabbitMQConsumerClientFactory(RabbitMQOptions rabbitMQOptions, IConnection connection)
        {
            _rabbitMQOptions = rabbitMQOptions;
            _connection = connection;
        }

        public IConsumerClient Create(string groupId)
        {
            return new RabbitMQConsumerClient(groupId, _connection, _rabbitMQOptions);
        }
    }
}
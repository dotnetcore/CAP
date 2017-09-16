using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly ConnectionPool _connectionPool;


        public RabbitMQConsumerClientFactory(RabbitMQOptions rabbitMQOptions, ConnectionPool pool)
        {
            _rabbitMQOptions = rabbitMQOptions;
            _connectionPool = pool;
        }

        public IConsumerClient Create(string groupId)
        {
            return new RabbitMQConsumerClient(groupId, _connectionPool, _rabbitMQOptions);
        }
    }
}
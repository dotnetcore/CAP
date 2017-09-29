namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        private readonly ConnectionPool _connectionPool;
        private readonly RabbitMQOptions _rabbitMQOptions;


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
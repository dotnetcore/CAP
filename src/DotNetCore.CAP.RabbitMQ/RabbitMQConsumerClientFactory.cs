using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.RabbitMQ
{
    public class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        private readonly RabbitMQOptions _rabbitMQOptions;

        public RabbitMQConsumerClientFactory(IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _rabbitMQOptions = rabbitMQOptions.Value;
        }

        public IConsumerClient Create(string groupId)
        {
            return new RabbitMQConsumerClient(groupId, _rabbitMQOptions);
        }
    }
}
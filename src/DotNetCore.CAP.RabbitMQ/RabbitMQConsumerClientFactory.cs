namespace DotNetCore.CAP.RabbitMQ
{
    public class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        public IConsumerClient Create(string groupId, string clientHostAddress)
        {
            return new RabbitMQConsumerClient(groupId, clientHostAddress);
        }
    }
}
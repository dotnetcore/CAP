namespace DotNetCore.CAP.Kafka
{
    public class KafkaConsumerClientFactory : IConsumerClientFactory
    {
        public IConsumerClient Create(string groupId, string clientHostAddress)
        {
            return new KafkaConsumerClient(groupId, clientHostAddress);
        }
    }
}
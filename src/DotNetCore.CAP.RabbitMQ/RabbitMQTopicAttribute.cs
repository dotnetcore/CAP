using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.RabbitMQ
{
    public class RabbitMQTopicAttribute : TopicAttribute
    {
        public RabbitMQTopicAttribute(string routingKey) : base(routingKey)
        {
        }
    }
}
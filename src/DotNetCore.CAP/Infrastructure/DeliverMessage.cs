namespace DotNetCore.CAP.Infrastructure
{
    public class DeliverMessage
    {
        /// <summary>
        /// Kafka 对应 Topic name
        /// <para>
        /// RabbitMQ 对应 RoutingKey
        /// </para>
        /// </summary>
        public string MessageKey { get; set; }

        public byte[] Body { get; set; }

        public string Value { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Kafka
{
    public class KafkaOptions
    {
        /// <summary>
        ///     Gets the Kafka broker id.
        /// </summary>
        public int BrokerId { get; }

        /// <summary>
        ///     Gets the Kafka broker hostname.
        /// </summary>
        public string Host { get; }

        /// <summary>
        ///     Gets the Kafka broker port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Returns a JSON representation of the BrokerMetadata object.
        /// </summary>
        /// <returns>
        ///     A JSON representation of the BrokerMetadata object.
        /// </returns>
        public override string ToString()
            => $"{{ \"BrokerId\": {BrokerId}, \"Host\": \"{Host}\", \"Port\": {Port} }}";
    }
}

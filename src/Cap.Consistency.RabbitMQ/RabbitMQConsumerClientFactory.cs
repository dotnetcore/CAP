using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Consumer;

namespace Cap.Consistency.RabbitMQ
{
    public class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        public IConsumerClient Create(string groupId, string clientHostAddress) {
            return new RabbitMQConsumerClient(groupId, clientHostAddress);
        }
    }
}

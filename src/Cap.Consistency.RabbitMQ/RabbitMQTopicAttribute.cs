using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;

namespace Cap.Consistency.RabbitMQ
{
    public class RabbitMQTopicAttribute : TopicAttribute
    {

        public RabbitMQTopicAttribute(string routingKey) : base(routingKey) {

        }
    }
}

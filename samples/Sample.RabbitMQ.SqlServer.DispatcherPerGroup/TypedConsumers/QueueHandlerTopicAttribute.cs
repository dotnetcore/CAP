using System;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup.TypedConsumers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class QueueHandlerTopicAttribute : Attribute
    {
        public string Topic { get; }

        public QueueHandlerTopicAttribute(string topic)
        {
            Topic = topic;
        }
    }
}
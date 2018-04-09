using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerPublishEndEventData : BrokerPublishEventData
    {
        public TimeSpan Duration { get; }

        public BrokerPublishEndEventData(Guid operationId, string operation, string groupName, string brokerTopicName,
            string brokerTopicBody, DateTimeOffset startTime, TimeSpan duration)
            : base(operationId, operation, groupName, brokerTopicName, brokerTopicBody, startTime)
        {
            Duration = duration;
        }
    }
}

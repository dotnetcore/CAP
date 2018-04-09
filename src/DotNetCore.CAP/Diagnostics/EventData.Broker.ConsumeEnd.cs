using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerConsumeEndEventData : BrokerConsumeEventData
    {
        public TimeSpan Duration { get; }

        public BrokerConsumeEndEventData(Guid operationId, string operation, string groupName, string brokerTopicName,
            string brokerTopicBody, DateTimeOffset startTime, TimeSpan duration)
            : base(operationId, operation, groupName, brokerTopicName, brokerTopicBody, startTime)
        {
            Duration = duration;
        }
    }
}

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerConsumeEventData : BrokerEventData
    {
        public DateTimeOffset StartTime { get; }

        public BrokerConsumeEventData(Guid operationId, string operation, string groupName,
            string brokerTopicName, string brokerTopicBody, DateTimeOffset startTime)
            : base(operationId, operation, groupName, brokerTopicName, brokerTopicBody)
        {
            StartTime = startTime;
        }
    }
}

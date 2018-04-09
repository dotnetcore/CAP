using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerPublishEventData : BrokerEventData
    {
        public DateTimeOffset StartTime { get; }

        public BrokerPublishEventData(Guid operationId, string operation, string groupName,
            string brokerTopicName, string brokerTopicBody, DateTimeOffset startTime)
            : base(operationId, operation, groupName, brokerTopicName, brokerTopicBody)
        {
            StartTime = startTime;
        }
    }
}

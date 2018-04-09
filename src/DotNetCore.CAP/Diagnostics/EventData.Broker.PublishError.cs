using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerPublishErrorEventData : BrokerPublishEndEventData, IErrorEventData
    {
        public Exception Exception { get; }

        public BrokerPublishErrorEventData(Guid operationId, string operation, string groupName,
            string brokerTopicName, string brokerTopicBody, Exception exception, DateTimeOffset startTime, TimeSpan duration)
            : base(operationId, operation, groupName, brokerTopicName, brokerTopicBody, startTime, duration)
        {
            Exception = exception;
        }
    }
}

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerConsumeErrorEventData : BrokerConsumeEndEventData, IErrorEventData
    {
        public Exception Exception { get; }

        public BrokerConsumeErrorEventData(Guid operationId, string operation, string groupName,
            string brokerTopicName, string brokerTopicBody, Exception exception, DateTimeOffset startTime, TimeSpan duration)
            : base(operationId, operation, groupName, brokerTopicName, brokerTopicBody, startTime, duration)
        {
            Exception = exception;
        }
    }
}

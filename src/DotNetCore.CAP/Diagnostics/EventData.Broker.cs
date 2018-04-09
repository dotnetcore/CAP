using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerEventData : EventData
    {
        public string GroupName { get; set; }

        public string BrokerTopicBody { get; set; }

        public string BrokerTopicName { get; set; }

        public BrokerEventData(Guid operationId, string operation, string groupName, 
            string brokerTopicName, string brokerTopicBody)
            : base(operationId, operation)
        {
            GroupName = groupName;
            BrokerTopicName = brokerTopicName;
            BrokerTopicBody = brokerTopicBody;
        }
    }
}

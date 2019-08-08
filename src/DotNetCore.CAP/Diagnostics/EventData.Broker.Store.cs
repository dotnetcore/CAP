using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerStoreEventData : EventData
    {
        public BrokerStoreEventData(
            Guid operationId, 
            string operation,
            string messageName, 
            string messageContent) : base(operationId, operation)
        {
            MessageName = messageName;
            MessageContent = messageContent;
        }
        
        public string MessageName { get; set; }
        public string MessageContent { get; set; }
        
        public TracingHeaders Headers { get; set; } 
    }
}
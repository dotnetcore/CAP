using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class EventData
    {
        public EventData(Guid operationId, string operation)
        {
            OperationId = operationId;
            Operation = operation;
        }

        public Guid OperationId { get; set; }

        public string Operation { get; set; }
    }
}

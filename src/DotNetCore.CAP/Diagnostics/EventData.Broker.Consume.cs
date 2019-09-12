// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerConsumeEventData
    {
        public BrokerConsumeEventData(Guid operationId,string brokerAddress, TransportMessage message, DateTimeOffset startTime)
        {
            OperationId = operationId;
            StartTime = startTime;
            BrokerAddress = brokerAddress;
            Message = message;
        }

        public Guid OperationId { get; set; }

        public string BrokerAddress { get; set; }

        public TransportMessage Message { get; set; }

        public DateTimeOffset StartTime { get; }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerConsumeErrorEventData : IErrorEventData
    {
        public BrokerConsumeErrorEventData(Guid operationId, string brokerAddress, TransportMessage message, Exception exception)
        {
            OperationId = operationId;
            BrokerAddress = brokerAddress;
            Message = message;
            Exception = exception;
        }

        public Guid OperationId { get; set; }

        public string BrokerAddress { get; }

        public TransportMessage Message { get; }

        public Exception Exception { get; }
    }
}
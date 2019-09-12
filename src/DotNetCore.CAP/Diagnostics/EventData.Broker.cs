// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerEventData : EventData
    {
        public BrokerEventData(Guid operationId, string operation, string brokerAddress, Message message)
            : base(operationId, operation)
        {
            BrokerAddress = brokerAddress;

            Message = message;
        }

        public string BrokerAddress { get; set; }

        public Message Message { get; set; }
    }
}
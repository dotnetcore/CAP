// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerPublishEventData : BrokerEventData
    {
        public BrokerPublishEventData(Guid operationId, string operation, string brokerAddress,
             Message message , DateTimeOffset startTime)
            : base(operationId, operation, brokerAddress, message)
        {
            StartTime = startTime;
        }

        public DateTimeOffset StartTime { get; }
    }
}
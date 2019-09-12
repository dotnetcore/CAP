// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerPublishEndEventData : BrokerPublishEventData
    {
        public BrokerPublishEndEventData(Guid operationId, string operation, string brokerAddress,
             Message message, DateTimeOffset startTime, TimeSpan duration)
            : base(operationId, operation, brokerAddress, message, startTime)
        {
            Duration = duration;
        }

        public TimeSpan Duration { get; }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerConsumeEventData : BrokerEventData
    {
        public BrokerConsumeEventData(Guid operationId, string operation, string brokerAddress,
            string brokerTopicName, string brokerTopicBody, DateTimeOffset startTime)
            : base(operationId, operation, brokerAddress, brokerTopicName, brokerTopicBody)
        {
            StartTime = startTime;
        }

        public DateTimeOffset StartTime { get; }
    }
}
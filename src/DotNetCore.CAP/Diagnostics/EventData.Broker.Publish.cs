// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerPublishEventData : BrokerEventData
    {
        public BrokerPublishEventData(Guid operationId, string operation, string brokerAddress,
            string brokerTopicName, string brokerTopicBody, DateTimeOffset startTime)
            : base(operationId, operation, brokerAddress, brokerTopicName, brokerTopicBody)
        {
            StartTime = startTime;
        }

        public DateTimeOffset StartTime { get; }
    }
}
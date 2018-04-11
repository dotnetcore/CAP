// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerConsumeEndEventData : BrokerConsumeEventData
    {
        public BrokerConsumeEndEventData(Guid operationId, string operation, string brokerAddress,
            string brokerTopicName,
            string brokerTopicBody, DateTimeOffset startTime, TimeSpan duration)
            : base(operationId, operation, brokerAddress, brokerTopicName, brokerTopicBody, startTime)
        {
            Duration = duration;
        }

        public TimeSpan Duration { get; }
    }
}
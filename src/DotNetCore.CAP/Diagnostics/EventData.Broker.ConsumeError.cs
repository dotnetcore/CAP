// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerConsumeErrorEventData : BrokerConsumeEndEventData, IErrorEventData
    {
        public BrokerConsumeErrorEventData(Guid operationId, string operation, string brokerAddress,
            string brokerTopicName, string brokerTopicBody, Exception exception, DateTimeOffset startTime,
            TimeSpan duration)
            : base(operationId, operation, brokerAddress, brokerTopicName, brokerTopicBody, startTime, duration)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
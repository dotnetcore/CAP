// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerPublishErrorEventData : BrokerPublishEndEventData, IErrorEventData
    {
        public BrokerPublishErrorEventData(Guid operationId, string operation, string brokerAddress,
            string brokerTopicName, string brokerTopicBody, Exception exception, DateTimeOffset startTime,
            TimeSpan duration, int retries)
            : base(operationId, operation, brokerAddress, brokerTopicName, brokerTopicBody, startTime, duration)
        {
            Retries = retries;
            Exception = exception;
        }

        public int Retries { get; }
        public Exception Exception { get; }
    }
}
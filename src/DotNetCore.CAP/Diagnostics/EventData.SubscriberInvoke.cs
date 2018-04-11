// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class SubscriberInvokeEventData : EventData
    {
        public SubscriberInvokeEventData(Guid operationId,
            string operation,
            string methodName,
            string subscribeName,
            string subscribeGroup,
            string parameterValues,
            DateTimeOffset startTime)
            : base(operationId, operation)
        {
            MethodName = methodName;
            SubscribeName = subscribeName;
            SubscribeGroup = subscribeGroup;
            ParameterValues = parameterValues;
            StartTime = startTime;
        }

        public DateTimeOffset StartTime { get; }

        public string MethodName { get; set; }

        public string SubscribeName { get; set; }

        public string SubscribeGroup { get; set; }

        public string ParameterValues { get; set; }
    }
}
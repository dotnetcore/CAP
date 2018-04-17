// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class SubscriberInvokeEndEventData : SubscriberInvokeEventData
    {
        public SubscriberInvokeEndEventData(Guid operationId, string operation,
            string methodName, string subscribeName, string subscribeGroup,
            string parameterValues, DateTimeOffset startTime, TimeSpan duration)
            : base(operationId, operation, methodName, subscribeName, subscribeGroup, parameterValues, startTime)
        {
            Duration = duration;
        }

        public TimeSpan Duration { get; }
    }
}
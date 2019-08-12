// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class SubscriberInvokeErrorEventData : SubscriberInvokeEndEventData, IErrorEventData
    {
        public SubscriberInvokeErrorEventData(Guid operationId, string operation, string methodName,
            string subscribeName, string subscribeGroup, string parameterValues, Exception exception,
            DateTimeOffset startTime, TimeSpan duration, int retries) : base(operationId, operation, methodName, 
            subscribeName, subscribeGroup, parameterValues, startTime, duration)
        {
            Retries = retries;
            Exception = exception;
        }

        public int Retries { get; }
        public Exception Exception { get; }
    }
}
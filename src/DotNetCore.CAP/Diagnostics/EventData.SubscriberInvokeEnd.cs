using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class SubscriberInvokeEndEventData : SubscriberInvokeEventData
    {
        public TimeSpan Duration { get; }

        public SubscriberInvokeEndEventData(Guid operationId, string operation,
            string methodName, string subscribeName, string subscribeGroup,
            string parameterValues, DateTimeOffset startTime, TimeSpan duration)
            : base(operationId, operation, methodName, subscribeName, subscribeGroup, parameterValues, startTime)
        {
            Duration = duration;
        }
    }
}

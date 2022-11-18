// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using DotNetCore.CAP.Diagnostics;

namespace DotNetCore.CAP.Dashboard
{
    public class CapMetricsEventListener : EventListener
    {
        public const int HistorySize = 300;

        public Queue<int?> PublishedPerSec { get; } = new(HistorySize);
        //public Queue<double?> ConsumePerSec { get; } = new(HistorySize);
        public Queue<int?> InvokeSubscriberPerSec { get; } = new(HistorySize);
        public Queue<int?> InvokeSubscriberElapsedMs { get; } = new(HistorySize);

        public CapMetricsEventListener()
        {
            for (int i = 0; i < HistorySize; i++)
            {
                PublishedPerSec.Enqueue(0);
                InvokeSubscriberPerSec.Enqueue(0);
                InvokeSubscriberElapsedMs.Enqueue(null);
            } 
        } 

        public Queue<int?>[] GetRealTimeMetrics()
        {
            var warpArr = new Queue<int?>[4];

            var dateVal = (int)DateTimeOffset.Now.AddSeconds(-300).ToUnixTimeSeconds();
            warpArr[0] = new Queue<int?>(Enumerable.Range(dateVal, 300).Cast<int?>());
            warpArr[1] = PublishedPerSec;
            warpArr[2] = InvokeSubscriberPerSec;
            warpArr[3] = InvokeSubscriberElapsedMs;

            return warpArr;
        }

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (!source.Name.Equals(CapDiagnosticListenerNames.MetricListenerName))
            {
                return;
            }

            EnableEvents(source, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>()
            {
                //report interval
                ["EventCounterIntervalSec"] = "1"
            });
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!eventData.EventName!.Equals("EventCounters"))
            {
                return;
            }

            var payload = (IDictionary<string, object>)eventData.Payload![0]!;

            var val = payload.Values.ToArray();

            if ((string)val[0] == CapDiagnosticListenerNames.PublishedPerSec)
            {
                PublishedPerSec.Dequeue();
                PublishedPerSec.Enqueue(Convert.ToInt32(val[3]));
            }
            //else if ((string)val[0] == CapDiagnosticListenerNames.ConsumePerSec)
            //{
            //        ConsumePerSec.Dequeue();
            //        var v = (double)val[3];
            //        ConsumePerSec.Enqueue(v);
            //}
            else if ((string)val[0] == CapDiagnosticListenerNames.InvokeSubscriberPerSec)
            {
                InvokeSubscriberPerSec.Dequeue();
                InvokeSubscriberPerSec.Enqueue(Convert.ToInt32(val[3]));
            }
            else if ((string)val[0] == CapDiagnosticListenerNames.InvokeSubscriberElapsedMs)
            {
                InvokeSubscriberElapsedMs.Dequeue();
                var v = Convert.ToInt32(val[2]);
                InvokeSubscriberElapsedMs.Enqueue(v == 0 ? null : v);
            }
        }

    }
}

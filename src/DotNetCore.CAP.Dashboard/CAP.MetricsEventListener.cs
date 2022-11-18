// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using DotNetCore.CAP.Diagnostics;

namespace DotNetCore.CAP.Dashboard
{
    internal class CapMetricsEventListener : EventListener
    {
        public const int HistorySize = 300;

        public CircularBuffer<int?> PublishedPerSec { get; } = new(HistorySize);
        //public Queue<double?> ConsumePerSec { get; } = new(HistorySize);
        public CircularBuffer<int?> InvokeSubscriberPerSec { get; } = new(HistorySize);
        public CircularBuffer<int?> InvokeSubscriberElapsedMs { get; } = new(HistorySize);

        public CapMetricsEventListener()
        {
            for (int i = 0; i < HistorySize; i++)
            {
                PublishedPerSec.Add(0);
                InvokeSubscriberPerSec.Add(0);
                InvokeSubscriberElapsedMs.Add(null);
            }
        }

        public CircularBuffer<int?>[] GetRealTimeMetrics()
        {
            var warpArr = new CircularBuffer<int?>[4];

            var startTime = (int)DateTimeOffset.Now.AddSeconds(-300).ToUnixTimeSeconds();
            var endTime = startTime + 300;

            var timeSerials = new CircularBuffer<int?>(HistorySize);
            for (var j = startTime; j < endTime; j++)
            {
                timeSerials.Add(j);
            }

            warpArr[0] = timeSerials;
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
                PublishedPerSec.Add(Convert.ToInt32(val[3]));
            }
            //else if ((string)val[0] == CapDiagnosticListenerNames.ConsumePerSec)
            //{
            //        ConsumePerSec.Dequeue();
            //        var v = (double)val[3];
            //        ConsumePerSec.Enqueue(v);
            //}
            else if ((string)val[0] == CapDiagnosticListenerNames.InvokeSubscriberPerSec)
            {
                InvokeSubscriberPerSec.Add(Convert.ToInt32(val[3]));
            }
            else if ((string)val[0] == CapDiagnosticListenerNames.InvokeSubscriberElapsedMs)
            {
                var v = Convert.ToInt32(val[2]);
                InvokeSubscriberElapsedMs.Add(v == 0 ? null : v);
            }
        }

    }
}

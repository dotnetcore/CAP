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

        public Metrics LastMetrics { get; } = new();

        public Queue<Metrics> Queue = new(HistorySize);

        public CapMetricsEventListener()
        {
            var nowTime = DateTime.Now;
            for (int i = HistorySize; i > 0; i--)
            {
                Queue.Enqueue(new Metrics
                {
                    RecordTime = nowTime.AddSeconds(-i).ToString("yyyy/MM/dd HH:mm:ss"),
                    PublishedPerSec = 0,
                    ConsumePerSec = 0,
                    InvokeSubscriberPerSec = 0,
                    InvokeSubscriberElapsedMs = 0
                });
            }
        }

        public class Metrics
        {
            public string RecordTime { get; set; }
            public double PublishedPerSec { get; set; }
            public double ConsumePerSec { get; set; }
            public double InvokeSubscriberPerSec { get; set; }
            public double InvokeSubscriberElapsedMs { get; set; }
        }

        public struct Pair
        {
            public Pair(string date, double count)
            {
                Value = new[]
                {
                    date,
                    count.ToString()
                };
                Name = null;
            }

            public string Name { get; set; }
            public string[] Value { get; set; }
        }

        public class MetricsHistory
        {
            public string[] FiveMinutes { get; set; } = new string[HistorySize];
            public Pair[] PublishedPerSec { get; set; } = new Pair[HistorySize];
            public Pair[] ConsumePerSec { get; set; } = new Pair[HistorySize];
            public Pair[] InvokeSubscriberPerSec { get; set; } = new Pair[HistorySize];

            // public KeyValuePair<DateTime, int>[] InvokeSubscriberElapsedMs => new KeyValuePair<DateTime, int>[HistorySize];
        }

        public MetricsHistory GetHistory()
        {
            var arr = new Metrics[HistorySize];
            Queue.CopyTo(arr, 0);

            var history = new MetricsHistory();
            for (var i = 0; i < arr.Length; i++)
            {
                var cur = arr[i];

                history.FiveMinutes[i] = cur.RecordTime;
                history.PublishedPerSec[i] = new(cur.RecordTime, cur.PublishedPerSec);
                history.ConsumePerSec[i] = new(cur.RecordTime, cur.ConsumePerSec);
                history.InvokeSubscriberPerSec[i] = new(cur.RecordTime, cur.InvokeSubscriberPerSec);
                // history.InvokeSubscriberElapsedMs.Add(cur.RecordTime, cur.InvokeSubscriberElapsedMs);
            }
            return history;
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
                LastMetrics.RecordTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                LastMetrics.PublishedPerSec = (double)val[3];

            }
            else if ((string)val[0] == CapDiagnosticListenerNames.ConsumePerSec)
            {
                LastMetrics.RecordTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                LastMetrics.ConsumePerSec = (double)val[3];
            }
            else if ((string)val[0] == CapDiagnosticListenerNames.InvokeSubscriberPerSec)
            {
                LastMetrics.RecordTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                LastMetrics.InvokeSubscriberPerSec = (double)val[3];
            }
            else if ((string)val[0] == CapDiagnosticListenerNames.InvokeSubscriberElapsedMs)
            {
                LastMetrics.RecordTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                LastMetrics.InvokeSubscriberElapsedMs = (double)val[3];
            }
            else
            {
                return;
            }

            Queue.Enqueue(LastMetrics);

            while (Queue.Count > HistorySize)
            {
                Queue.Dequeue();
            }
        }

    }
}

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
        public Queue<double?> PublishedPerSec { get; } = new(HistorySize);
        public Queue<double?> ConsumePerSec { get; } = new(HistorySize);
        public Queue<double?> InvokeSubscriberElapsedMs { get; } = new(HistorySize);

        public CapMetricsEventListener()
        {
            for (int i = 0; i < HistorySize; i++)
            {
                PublishedPerSec.Enqueue(0);
                ConsumePerSec.Enqueue(0);
                InvokeSubscriberElapsedMs.Enqueue(null);
            }

            //Task.Factory.StartNew(async () =>
            //{
            //    var token = lifetime.ApplicationStopping;
            //    while (!token.IsCancellationRequested)
            //    {
            //        await Task.Delay(1000, token);

            //        lock (Current)
            //        {
            //            PublishedPerSec.Dequeue();
            //            ConsumePerSec.Dequeue();
            //            InvokeSubscriberElapsedMs.Dequeue();

            //            PublishedPerSec.Enqueue(Current.PublishedPerSec);
            //            ConsumePerSec.Enqueue(Current.ConsumePerSec);
            //            InvokeSubscriberElapsedMs.Enqueue(Current.InvokeSubscriberPerSec);

            //            Current.PublishedPerSec = 0;
            //            Current.ConsumePerSec = 0;
            //            Current.InvokeSubscriberPerSec = 0;
            //        }
            //    }
            //});
        }

        //public class Metrics
        //{
        //    public double? PublishedPerSec { get; set; }
        //    public double? ConsumePerSec { get; set; }
        //    public double? InvokeSubscriberPerSec { get; set; }
        //    public double? InvokeSubscriberElapsedMs { get; set; }
        //}

        public double?[][] GetRealTimeMetrics()
        {
            var warpArr = new double?[4][];
            warpArr[0] = new double?[HistorySize];  //x-timestamps
            //warpArr[1] = new double[HistorySize];  //y1-publish
            //warpArr[2] = new double[HistorySize];  //y2-consume
            //warpArr[3] = new double[HistorySize];  //y3-subscriber

            var dateVal = DateTimeOffset.Now.AddSeconds(-300).ToUnixTimeSeconds();
            for (long i = 0, j = dateVal; j < dateVal + 300; i++, j++)
            {
                warpArr[0][i] = j;
            }
            warpArr[1] = PublishedPerSec.ToArray();
            warpArr[2] = ConsumePerSec.ToArray();
            warpArr[3] = InvokeSubscriberElapsedMs.ToArray();

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
                    var v = (double)val[3];
                    PublishedPerSec.Enqueue(v);
            }
            else if ((string)val[0] == CapDiagnosticListenerNames.ConsumePerSec)
            {
                    ConsumePerSec.Dequeue();
                    var v = (double)val[3];
                    ConsumePerSec.Enqueue(v);
            }
            //else if ((string)val[0] == CapDiagnosticListenerNames.InvokeSubscriberPerSec)
            //{
            //    lock (Current)
            //    {
            //        Current.InvokeSubscriberPerSec = (double)val[3];
            //    }
            //}
            else if ((string)val[0] == CapDiagnosticListenerNames.InvokeSubscriberElapsedMs)
            {
                    InvokeSubscriberElapsedMs.Dequeue();
                    var v = (double)val[2];
                    InvokeSubscriberElapsedMs.Enqueue(v == 0 ? null : v);
            }
            else
            {
                return;
            }
        }

    }
}

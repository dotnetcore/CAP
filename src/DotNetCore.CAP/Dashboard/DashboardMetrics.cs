// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.CAP.Dashboard.Resources;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Dashboard
{
    public static class DashboardMetrics
    {
        private static readonly Dictionary<string, DashboardMetric> Metrics = new Dictionary<string, DashboardMetric>();

        public static readonly DashboardMetric ServerCount = new DashboardMetric(
            "servers:count",
            "Metrics_Servers",
            page => new Metric(page.Statistics.Servers.ToString("N0"))
            {
                Style = page.Statistics.Servers == 0 ? MetricStyle.Warning : MetricStyle.Default,
                Highlighted = page.Statistics.Servers == 0,
                Title = page.Statistics.Servers == 0
                    ? "No active servers found. Jobs will not be processed."
                    : null
            });

        public static readonly DashboardMetric SubscriberCount = new DashboardMetric(
            "retries:count",
            "Metrics_Retries",
            page =>
            {
                long retryCount;
                var methodCache = page.RequestServices.GetService<MethodMatcherCache>();
                retryCount = methodCache.GetCandidatesMethodsOfGroupNameGrouped().Sum(x => x.Value.Count);

                return new Metric(retryCount.ToString("N0"))
                {
                    Style = retryCount > 0 ? MetricStyle.Default : MetricStyle.Warning
                };
            });

        //----------------------------------------------------

        public static readonly DashboardMetric PublishedFailedCountOrNull = new DashboardMetric(
            "published_failed:count-or-null",
            "Metrics_FailedJobs",
            page => page.Statistics.PublishedFailed > 0
                ? new Metric(page.Statistics.PublishedFailed.ToString("N0"))
                {
                    Style = MetricStyle.Danger,
                    Highlighted = true,
                    Title = string.Format(Strings.Metrics_FailedCountOrNull, page.Statistics.PublishedFailed)
                }
                : null);

        public static readonly DashboardMetric ReceivedFailedCountOrNull = new DashboardMetric(
            "received_failed:count-or-null",
            "Metrics_FailedJobs",
            page => page.Statistics.ReceivedFailed > 0
                ? new Metric(page.Statistics.ReceivedFailed.ToString("N0"))
                {
                    Style = MetricStyle.Danger,
                    Highlighted = true,
                    Title = string.Format(Strings.Metrics_FailedCountOrNull, page.Statistics.ReceivedFailed)
                }
                : null);

        //----------------------------------------------------
        public static readonly DashboardMetric PublishedSucceededCount = new DashboardMetric(
            "published_succeeded:count",
            "Metrics_SucceededJobs",
            page => new Metric(page.Statistics.PublishedSucceeded.ToString("N0"))
            {
                IntValue = page.Statistics.PublishedSucceeded
            });

        public static readonly DashboardMetric ReceivedSucceededCount = new DashboardMetric(
            "received_succeeded:count",
            "Metrics_SucceededJobs",
            page => new Metric(page.Statistics.ReceivedSucceeded.ToString("N0"))
            {
                IntValue = page.Statistics.ReceivedSucceeded
            });

        //----------------------------------------------------

        public static readonly DashboardMetric PublishedFailedCount = new DashboardMetric(
            "published_failed:count",
            "Metrics_FailedJobs",
            page => new Metric(page.Statistics.PublishedFailed.ToString("N0"))
            {
                IntValue = page.Statistics.PublishedFailed,
                Style = page.Statistics.PublishedFailed > 0 ? MetricStyle.Danger : MetricStyle.Default,
                Highlighted = page.Statistics.PublishedFailed > 0
            });

        public static readonly DashboardMetric ReceivedFailedCount = new DashboardMetric(
            "received_failed:count",
            "Metrics_FailedJobs",
            page => new Metric(page.Statistics.ReceivedFailed.ToString("N0"))
            {
                IntValue = page.Statistics.ReceivedFailed,
                Style = page.Statistics.ReceivedFailed > 0 ? MetricStyle.Danger : MetricStyle.Default,
                Highlighted = page.Statistics.ReceivedFailed > 0
            });

        static DashboardMetrics()
        {
            AddMetric(ServerCount);
            AddMetric(SubscriberCount);

            AddMetric(PublishedFailedCountOrNull);
            AddMetric(ReceivedFailedCountOrNull);

            AddMetric(PublishedSucceededCount);
            AddMetric(ReceivedSucceededCount);

            AddMetric(PublishedFailedCount);
            AddMetric(ReceivedFailedCount);
        }

        public static void AddMetric(DashboardMetric metric)
        {
            if (metric == null)
            {
                throw new ArgumentNullException(nameof(metric));
            }

            lock (Metrics)
            {
                Metrics[metric.Name] = metric;
            }
        }

        public static IEnumerable<DashboardMetric> GetMetrics()
        {
            lock (Metrics)
            {
                return Metrics.Values.ToList();
            }
        }
    }
}
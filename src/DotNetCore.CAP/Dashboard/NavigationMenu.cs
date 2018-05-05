// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DotNetCore.CAP.Dashboard.Resources;

namespace DotNetCore.CAP.Dashboard
{
    public static class NavigationMenu
    {
        public static readonly List<Func<RazorPage, MenuItem>> Items = new List<Func<RazorPage, MenuItem>>();

        static NavigationMenu()
        {
            Items.Add(page => new MenuItem(Strings.NavigationMenu_Published, page.Url.LinkToPublished())
            {
                Active = page.RequestPath.StartsWith("/published"),
                Metrics = new[]
                {
                    DashboardMetrics.PublishedSucceededCount,
                    DashboardMetrics.PublishedFailedCountOrNull
                }
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_Received, page.Url.LinkToReceived())
            {
                Active = page.RequestPath.StartsWith("/received"),
                Metrics = new[]
                {
                    DashboardMetrics.ReceivedSucceededCount,
                    DashboardMetrics.ReceivedFailedCountOrNull
                }
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_Subscribers, page.Url.To("/subscribers"))
            {
                Active = page.RequestPath.StartsWith("/subscribers"),
                Metric = DashboardMetrics.SubscriberCount
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_Servers, page.Url.To("/nodes"))
            {
                Active = page.RequestPath.Equals("/nodes"),
                Metric = DashboardMetrics.ServerCount
            });
        }
    }
}
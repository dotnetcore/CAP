// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DotNetCore.CAP.Dashboard.Resources;

namespace DotNetCore.CAP.Dashboard
{
    public static class MessagesSidebarMenu
    {
        public static readonly List<Func<RazorPage, MenuItem>> PublishedItems
            = new List<Func<RazorPage, MenuItem>>();

        public static readonly List<Func<RazorPage, MenuItem>> ReceivedItems
            = new List<Func<RazorPage, MenuItem>>();

        static MessagesSidebarMenu()
        {
            PublishedItems.Add(page => new MenuItem(Strings.SidebarMenu_Succeeded, page.Url.To("/published/succeeded"))
            {
                Active = page.RequestPath.StartsWith("/published/succeeded"),
                Metric = DashboardMetrics.PublishedSucceededCount
            });

            PublishedItems.Add(page => new MenuItem(Strings.SidebarMenu_Failed, page.Url.To("/published/failed"))
            {
                Active = page.RequestPath.StartsWith("/published/failed"),
                Metric = DashboardMetrics.PublishedFailedCount
            });

            //=======================================ReceivedItems=============================

            ReceivedItems.Add(page => new MenuItem(Strings.SidebarMenu_Succeeded, page.Url.To("/received/succeeded"))
            {
                Active = page.RequestPath.StartsWith("/received/succeeded"),
                Metric = DashboardMetrics.ReceivedSucceededCount
            });

            ReceivedItems.Add(page => new MenuItem(Strings.SidebarMenu_Failed, page.Url.To("/received/failed"))
            {
                Active = page.RequestPath.StartsWith("/received/failed"),
                Metric = DashboardMetrics.ReceivedFailedCount
            });
        }
    }
}
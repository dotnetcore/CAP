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
            Items.Add(page => new MenuItem(Strings.NavigationMenu_Jobs, page.Url.LinkToQueues())
            {
                Active = page.RequestPath.StartsWith("/jobs"),
                Metrics = new []
                {
                    DashboardMetrics.EnqueuedCountOrNull,
                    DashboardMetrics.FailedCountOrNull
                }
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_Retries, page.Url.To("/retries"))
            {
                Active = page.RequestPath.StartsWith("/retries"),
                Metric = DashboardMetrics.RetriesCount
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_RecurringJobs, page.Url.To("/recurring"))
            {
                Active = page.RequestPath.StartsWith("/recurring"),
                Metric = DashboardMetrics.RecurringJobCount
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_Servers, page.Url.To("/servers"))
            {
                Active = page.RequestPath.Equals("/servers"),
                Metric = DashboardMetrics.ServerCount
            });
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Dashboard
{
    public class DashboardMetric
    {
        public DashboardMetric(string name, Func<RazorPage, Metric> func)
            : this(name, name, func)
        {
        }

        public DashboardMetric(string name, string title, Func<RazorPage, Metric> func)
        {
            Name = name;
            Title = title;
            Func = func;
        }

        public string Name { get; }
        public Func<RazorPage, Metric> Func { get; }

        public string Title { get; set; }
    }
}
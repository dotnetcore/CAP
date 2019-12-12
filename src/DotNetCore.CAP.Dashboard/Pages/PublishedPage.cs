// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Monitoring;

namespace DotNetCore.CAP.Dashboard.Pages
{
    internal partial class PublishedPage : DotNetCore.CAP.Dashboard.RazorPage
    {
        public PublishedPage(string statusName)
        {
            Name = statusName;
        }

        public string Name { get; set; }

        public int GetTotal(IMonitoringApi api)
        {
            if (string.Equals(Name, nameof(Internal.StatusName.Succeeded),
                StringComparison.CurrentCultureIgnoreCase))
            {
                return api.PublishedSucceededCount();
            }

            return api.PublishedFailedCount();
        }
    }
}
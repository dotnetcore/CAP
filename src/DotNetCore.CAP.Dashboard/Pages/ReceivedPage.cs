﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Monitoring;

namespace DotNetCore.CAP.Dashboard.Pages
{
    internal partial class ReceivedPage
    {
        public ReceivedPage(string statusName)
        {
            StatusName = statusName;
        }

        public string StatusName { get; set; }

        public int GetTotal(IMonitoringApi api)
        {
            if (string.Equals(StatusName, nameof(Internal.StatusName.Succeeded),
                StringComparison.CurrentCultureIgnoreCase))
            {
                return api.ReceivedSucceededCount();
            }

            return api.ReceivedFailedCount();
        }
    }
}
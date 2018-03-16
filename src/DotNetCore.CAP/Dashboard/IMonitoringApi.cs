// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard
{
    public interface IMonitoringApi
    {
        StatisticsDto GetStatistics();

        IList<MessageDto> Messages(MessageQueryDto queryDto);

        int PublishedFailedCount();

        int PublishedSucceededCount();

        int ReceivedFailedCount();

        int ReceivedSucceededCount();

        IDictionary<DateTime, int> HourlySucceededJobs(MessageType type);

        IDictionary<DateTime, int> HourlyFailedJobs(MessageType type);
    }
}
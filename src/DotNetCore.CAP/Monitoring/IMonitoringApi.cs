// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Monitoring
{
    public interface IMonitoringApi
    {
        Task<MediumMessage?> GetPublishedMessageAsync(long id);

        Task<MediumMessage?> GetReceivedMessageAsync(long id);

        StatisticsDto GetStatistics();

        PagedQueryResult<MessageDto> Messages(MessageQueryDto queryDto);

        int PublishedFailedCount();

        int PublishedSucceededCount();

        int ReceivedFailedCount();

        int ReceivedSucceededCount();

        IDictionary<DateTime, int> HourlySucceededJobs(MessageType type);

        IDictionary<DateTime, int> HourlyFailedJobs(MessageType type);
    }
}
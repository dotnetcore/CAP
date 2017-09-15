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

        int PublishedProcessingCount();

        int PublishedSucceededCount();

        int ReceivedFailedCount();

        int ReceivedProcessingCount();

        int ReceivedSucceededCount();

        IDictionary<DateTime, int> HourlySucceededJobs(MessageType type);

        IDictionary<DateTime, int> HourlyFailedJobs(MessageType type);
    }
}
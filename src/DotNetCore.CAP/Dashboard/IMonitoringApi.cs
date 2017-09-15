using System;
using System.Collections.Generic;
using DotNetCore.CAP.Dashboard.Monitoring;

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

        IDictionary<DateTime, int> SucceededByDatesCount();

        IDictionary<DateTime, int> FailedByDatesCount();

        IDictionary<DateTime, int> HourlySucceededJobs();

        IDictionary<DateTime, int> HourlyFailedJobs();
    }
}
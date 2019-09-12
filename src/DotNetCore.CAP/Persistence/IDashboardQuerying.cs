//using System;
//using System.Collections.Generic;
//using DotNetCore.CAP.Dashboard.Monitoring;
//using DotNetCore.CAP.Messages;

//namespace DotNetCore.CAP.Persistence
//{
//    public interface IDashboardQuerying
//    {
//        StatisticsDto GetStatistics();

//        IList<MessageDto> Messages(MessageQueryDto queryDto);

//        int PublishedFailedCount();

//        int PublishedSucceededCount();

//        int ReceivedFailedCount();

//        int ReceivedSucceededCount();

//        IDictionary<DateTime, int> HourlySucceededJobs(MessageType type);

//        IDictionary<DateTime, int> HourlyFailedJobs(MessageType type);
//    }
//}

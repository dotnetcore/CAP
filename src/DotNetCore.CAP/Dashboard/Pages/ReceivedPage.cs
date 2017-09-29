using System;
using DotNetCore.CAP.Processor.States;

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
            if (string.Equals(StatusName, SucceededState.StateName, StringComparison.CurrentCultureIgnoreCase))
            {
                return api.ReceivedSucceededCount();
            }
            if (string.Equals(StatusName, ProcessingState.StateName, StringComparison.CurrentCultureIgnoreCase))
            {
                return api.ReceivedProcessingCount();
            }
            return api.ReceivedFailedCount();
        }
    }
}
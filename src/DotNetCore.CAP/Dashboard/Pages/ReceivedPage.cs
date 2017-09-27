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
            if (string.Compare(StatusName, SucceededState.StateName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return api.ReceivedSucceededCount();
            }
            if (string.Compare(StatusName, ProcessingState.StateName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return api.ReceivedProcessingCount();
            }
            return api.ReceivedFailedCount();
        }
    }
}
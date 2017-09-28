using System;
using DotNetCore.CAP.Processor.States;

namespace DotNetCore.CAP.Dashboard.Pages
{
    internal partial class PublishedPage
    {
        public PublishedPage(string statusName)
        {
            StatusName = statusName;
        }

        public string StatusName { get; set; }

        public int GetTotal(IMonitoringApi api)
        {
            if (string.Equals(StatusName, SucceededState.StateName, StringComparison.CurrentCultureIgnoreCase))
            {
                return api.PublishedSucceededCount();
            }
            if (string.Equals(StatusName, ProcessingState.StateName, StringComparison.CurrentCultureIgnoreCase))
            {
                return api.PublishedProcessingCount();
            }
            return api.PublishedFailedCount();
        }
    }
}
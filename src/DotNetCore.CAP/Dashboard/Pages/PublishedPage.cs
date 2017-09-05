using System;
using System.Collections.Generic;
using System.Text;
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
            if (String.Compare(StatusName, SucceededState.StateName, true) == 0)
            {
                return api.PublishedSucceededCount();
            }
            else if (String.Compare(StatusName, ProcessingState.StateName, true) == 0)
            {
                return api.PublishedProcessingCount();
            }
            else
            {
                return api.PublishedFailedCount();
            }
        }
    }
}

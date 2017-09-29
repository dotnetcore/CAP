using System;

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
            if (string.Equals(StatusName, Infrastructure.StatusName.Succeeded,
                StringComparison.CurrentCultureIgnoreCase))
                return api.ReceivedSucceededCount();
            if (string.Equals(StatusName, Infrastructure.StatusName.Processing,
                StringComparison.CurrentCultureIgnoreCase))
                return api.ReceivedProcessingCount();
            return api.ReceivedFailedCount();
        }
    }
}
using System;

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
            if (string.Equals(StatusName, Infrastructure.StatusName.Succeeded,
                StringComparison.CurrentCultureIgnoreCase))
                return api.PublishedSucceededCount();
            if (string.Equals(StatusName, Infrastructure.StatusName.Processing,
                StringComparison.CurrentCultureIgnoreCase))
                return api.PublishedProcessingCount();
            return api.PublishedFailedCount();
        }
    }
}
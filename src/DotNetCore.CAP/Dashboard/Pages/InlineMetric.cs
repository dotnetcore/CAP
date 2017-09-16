namespace DotNetCore.CAP.Dashboard.Pages
{
    internal partial class InlineMetric
    {
        public InlineMetric(DashboardMetric dashboardMetric)
        {
            DashboardMetric = dashboardMetric;
        }

        public DashboardMetric DashboardMetric { get; }
    }
}
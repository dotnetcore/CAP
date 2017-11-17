namespace DotNetCore.CAP.Dashboard
{
    public interface IDashboardAuthorizationFilter
    {
        bool Authorize(DashboardContext context);
    }
}
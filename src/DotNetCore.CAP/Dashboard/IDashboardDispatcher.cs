using System.Threading.Tasks;

namespace DotNetCore.CAP.Dashboard
{
    public interface IDashboardDispatcher
    {
        Task Dispatch(DashboardContext context);
    }
}
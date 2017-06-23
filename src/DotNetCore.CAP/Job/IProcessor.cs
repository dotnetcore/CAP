using System.Threading.Tasks;

namespace DotNetCore.CAP.Job
{
    public interface IJobProcessor
    {
        Task ProcessAsync(ProcessingContext context);
    }
}
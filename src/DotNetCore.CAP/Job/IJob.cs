using System.Threading.Tasks;

namespace DotNetCore.CAP.Job
{
    public interface IJob
    {
        /// <summary>
        /// Executes the job.
        /// </summary>
        Task ExecuteAsync();
    }
}
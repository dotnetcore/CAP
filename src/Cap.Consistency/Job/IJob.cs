using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Job
{
    public interface IJob
    {
        /// <summary>
        /// Executes the job.
        /// </summary>
        Task ExecuteAsync();
    }
}

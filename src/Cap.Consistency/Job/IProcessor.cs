using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Job
{
    public interface IJobProcessor
    {
        Task ProcessAsync(ProcessingContext context);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Job
{
    public interface IProcessor
    {
        Task ProcessAsync(ProcessingContext context);
    }
}

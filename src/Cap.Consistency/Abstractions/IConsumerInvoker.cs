using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Abstractions
{
    public interface IConsumerInvoker
    {
        Task InvokeAsync();
    }
}

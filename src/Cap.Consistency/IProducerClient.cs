using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Infrastructure;

namespace Cap.Consistency
{
    public interface IProducerClient
    {
        Task SendAsync(string topic, string content);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Producer
{
    public interface IProducerClient
    {
        Task SendAsync(string topic, string content);
    }
}

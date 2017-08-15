using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    public interface ICallbackPublisher
    {
        Task PublishAsync(string name, object obj);
    }
}

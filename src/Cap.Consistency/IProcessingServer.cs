using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency
{
    public interface IProcessingServer : IDisposable
    {
        void Start();
    }
}

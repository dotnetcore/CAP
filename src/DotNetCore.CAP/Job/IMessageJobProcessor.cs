using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Job
{
    public interface IMessageJobProcessor : IJobProcessor
    {
        bool Waiting { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Processor
{
    public interface IMessageJobProcessor : IJobProcessor
    {
        bool Waiting { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Processor
{
    public interface IDispatcher : IProcessor
    {
        bool Waiting { get; }
    }
}

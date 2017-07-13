using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Processor
{
    public interface IMessageProcessor : IProcessor
    {
        bool Waiting { get; }
    }
}

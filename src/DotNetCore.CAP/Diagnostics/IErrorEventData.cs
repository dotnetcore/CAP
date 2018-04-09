using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Diagnostics
{
    public interface IErrorEventData
    {
        Exception Exception { get; }
    }
}

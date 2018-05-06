using Microsoft.EntityFrameworkCore.Diagnostics;
using SkyWalking.Context.Trace;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public interface IEfCoreSpanFactory
    {
        ISpan Create(string operationName, CommandEventData eventData);
    }
}
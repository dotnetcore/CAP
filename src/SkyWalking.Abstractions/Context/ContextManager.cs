using System;
using System.Threading;

namespace SkyWalking.Context
{
    /// <summary>
    /// Context manager controls the whole context of tracing. Since .NET server application runs as same as Java,
    /// We also provide the CONTEXT propagation based on ThreadLocal mechanism.
    /// Meaning, each segment also related to singe thread.
    /// </summary>
    public static class ContextManager
    {
        private static readonly ThreadLocal<ITracerContext> CONTEXT = new ThreadLocal<ITracerContext>();

        private static ITracerContext GetOrCreate(String operationName, bool forceSampling)
        {
            if (!CONTEXT.IsValueCreated)
            {
                return null;
            }
            else
            {
                return null;
            }

        }
    }
}

using System;
using SkyWalking.Context;

namespace SkyWalking.Diagnostics.EntityFrameworkCore.Tests.Fakes
{
    public class FakeIgnoreTracerContextListener : IIgnoreTracerContextListener ,IDisposable
    {
        public int Counter { get; set; }

        public FakeIgnoreTracerContextListener()
        {
            IgnoredTracerContext.ListenerManager.Add(this);
        }

        public void AfterFinish(ITracerContext tracerContext)
        {
            Counter = Counter + 1;
        }

        public void Dispose()
        {
            IgnoredTracerContext.ListenerManager.Remove(this);
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;

namespace Cap.Consistency.Consumer
{
    public interface ITaskSchedule 
    {
        void Start(IReadOnlyList<ConsumerExecutorDescriptor> methods);

        void Stop();
    }
}

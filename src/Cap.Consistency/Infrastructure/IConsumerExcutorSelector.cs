using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Routing;

namespace Cap.Consistency.Infrastructure
{
    public interface IConsumerExcutorSelector
    {
        IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates(TopicRouteContext context);

        ConsumerExecutorDescriptor SelectBestCandidate(string key, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor);
    }
}

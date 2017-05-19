using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Route;

namespace Cap.Consistency.Infrastructure
{
    public interface IConsumerExcutorSelector
    {
        IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates(TopicRouteContext context);

        ConsumerExecutorDescriptor SelectBestCandidate(TopicRouteContext context, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;

namespace Cap.Consistency.Infrastructure
{
    public interface IConsumerExcutorSelector
    {
        IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates(TopicContext context);

        ConsumerExecutorDescriptor SelectBestCandidate(string key, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor);
    }
}

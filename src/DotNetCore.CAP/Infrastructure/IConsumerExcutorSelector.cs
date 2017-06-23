using System.Collections.Generic;
using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Infrastructure
{
    public interface IConsumerExcutorSelector
    {
        IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates(TopicContext context);

        ConsumerExecutorDescriptor SelectBestCandidate(string key, IReadOnlyList<ConsumerExecutorDescriptor> executeDescriptor);
    }
}
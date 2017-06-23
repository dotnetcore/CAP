using System;
using System.Collections.Concurrent;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Internal
{
    public class MethodMatcherCache
    {
        private readonly IConsumerServiceSelector _selector;

        public MethodMatcherCache(IConsumerServiceSelector selector)
        {
            _selector = selector;
        }

        public ConcurrentDictionary<string, ConsumerExecutorDescriptor> GetCandidatesMethods(CapStartContext routeContext)
        {
            if (Entries.Count == 0)
            {
                var executorCollection = _selector.SelectCandidates(routeContext);

                foreach (var item in executorCollection)
                {
                    Entries.GetOrAdd(item.Attribute.Name, item);
                }
            }
            return Entries;
        }

        public ConsumerExecutorDescriptor GetTopicExector(string topicName)
        {
            if (Entries == null)
            {
                throw new ArgumentNullException(nameof(Entries));
            }

            return Entries[topicName];
        }

        public ConcurrentDictionary<string, ConsumerExecutorDescriptor> Entries { get; } =
               new ConcurrentDictionary<string, ConsumerExecutorDescriptor>();
    }
}
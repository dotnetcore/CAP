using System;
using System.Collections.Concurrent;
using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Internal
{
    public class MethodMatcherCache
    {
        private readonly IConsumerServiceSelector _selector;

        public MethodMatcherCache(IConsumerServiceSelector selector)
        {
            _selector = selector;
        }

        public ConcurrentDictionary<string, ConsumerExecutorDescriptor> GetCandidatesMethods(IServiceProvider provider)
        {
            if (Entries.Count != 0) return Entries;

            var executorCollection = _selector.SelectCandidates(provider);

            foreach (var item in executorCollection)
            {
                Entries.GetOrAdd(item.Attribute.Name, item);
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
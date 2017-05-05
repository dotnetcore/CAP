using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;
using Cap.Consistency.Infrastructure;
using Cap.Consistency.Routing;

namespace Cap.Consistency.Internal
{
    public class MethodMatcherCache
    {
        private readonly IConsumerExcutorSelector _selector;

        public MethodMatcherCache(IConsumerExcutorSelector selector) {
            _selector = selector;
        }

        public ConcurrentDictionary<string, ConsumerExecutorDescriptor> GetCandidatesMethods(TopicRouteContext routeContext) {

            if (Entries == null) {

                var executorCollection = _selector.SelectCandidates(routeContext);

                foreach (var item in executorCollection) {

                    Entries.GetOrAdd(item.Topic.Name, item);
                }
            }
            return Entries;
        }

        public ConsumerExecutorDescriptor GetTopicExector(string topicName) {

            if (Entries == null) {
                throw new ArgumentNullException(nameof(Entries));
            }

            return Entries[topicName];
        }

        public ConcurrentDictionary<string, ConsumerExecutorDescriptor> Entries { get; } =
               new ConcurrentDictionary<string, ConsumerExecutorDescriptor>();
    }
}

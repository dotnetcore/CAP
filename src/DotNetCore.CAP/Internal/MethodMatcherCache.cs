using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.CAP.Internal
{
    internal class MethodMatcherCache
    {
        private readonly IConsumerServiceSelector _selector;
        private List<string> _allTopics;

        public MethodMatcherCache(IConsumerServiceSelector selector)
        {
            _selector = selector;
            Entries = new ConcurrentDictionary<string, IList<ConsumerExecutorDescriptor>>();
        }

        private ConcurrentDictionary<string, IList<ConsumerExecutorDescriptor>> Entries { get; }

        /// <summary>
        /// Get a dictionary of candidates.In the dictionary,
        /// the Key is the CAPSubscribeAttribute Group, the Value for the current Group of candidates
        /// </summary>
        public ConcurrentDictionary<string, IList<ConsumerExecutorDescriptor>> GetCandidatesMethodsOfGroupNameGrouped()
        {
            if (Entries.Count != 0) return Entries;

            var executorCollection = _selector.SelectCandidates();

            var groupedCandidates = executorCollection.GroupBy(x => x.Attribute.Group);

            foreach (var item in groupedCandidates)
                Entries.TryAdd(item.Key, item.ToList());

            return Entries;
        }

        /// <summary>
        /// Get a dictionary of specify topic candidates.
        /// The Key is Group name, the value is specify topic candidates.
        /// </summary>
        /// <param name="topicName">message topic name</param>
        public IDictionary<string, IList<ConsumerExecutorDescriptor>> GetTopicExector(string topicName)
        {
            if (Entries == null)
                throw new ArgumentNullException(nameof(Entries));

            var dic = new Dictionary<string, IList<ConsumerExecutorDescriptor>>();
            foreach (var item in Entries)
            {
                var topicCandidates = item.Value.Where(x => x.Attribute.Name == topicName);
                dic.Add(item.Key, topicCandidates.ToList());
            }
            return dic;
        }

        /// <summary>
        /// Get all subscribe topics name.
        /// </summary>
        public IEnumerable<string> GetSubscribeTopics()
        {
            if (_allTopics != null)
            {
                return _allTopics;
            }

            if (Entries == null)
                throw new ArgumentNullException(nameof(Entries));

            _allTopics = new List<string>();

            foreach (var descriptors in Entries.Values)
            {
                _allTopics.AddRange(descriptors.Select(x => x.Attribute.Name));
            }
            return _allTopics;
        }
    }
}
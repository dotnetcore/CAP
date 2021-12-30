// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DotNetCore.CAP.Internal
{
    public class MethodMatcherCache
    {
        private readonly IConsumerServiceSelector _selector;

        public MethodMatcherCache(IConsumerServiceSelector selector)
        {
            _selector = selector;
            Entries = new ConcurrentDictionary<string, IReadOnlyList<ConsumerExecutorDescriptor>>();
        }

        private ConcurrentDictionary<string, IReadOnlyList<ConsumerExecutorDescriptor>> Entries { get; }

        /// <summary>
        /// Get a dictionary of candidates.In the dictionary,
        /// the Key is the CAPSubscribeAttribute Group, the Value for the current Group of candidates
        /// </summary>
        public ConcurrentDictionary<string, IReadOnlyList<ConsumerExecutorDescriptor>> GetCandidatesMethodsOfGroupNameGrouped()
        {
            if (Entries.Count != 0)
            {
                return Entries;
            }

            var executorCollection = _selector.SelectCandidates();

            var groupedCandidates = executorCollection.GroupBy(x => x.Attribute.Group);

            foreach (var item in groupedCandidates)
            {
                Entries.TryAdd(item.Key, item.ToList());
            }

            return Entries;
        }

        public List<string> GetAllTopics()
        {
            if (Entries.Count == 0)
            {
                GetCandidatesMethodsOfGroupNameGrouped();
            }

            var result = new List<string>();
            foreach (var item in Entries.Values)
            {
                result.AddRange(item.Select(x => x.TopicName));
            }
            return result;
        }

        /// <summary>
        /// Attempts to get the topic executor associated with the specified topic name and group name from the
        /// <see cref="Entries" />.
        /// </summary>
        /// <param name="topicName">The topic name of the value to get.</param>
        /// <param name="groupName">The group name of the value to get.</param>
        /// <param name="matchTopic">topic executor of the value.</param>
        /// <returns>true if the key was found, otherwise false. </returns>
        public bool TryGetTopicExecutor(string topicName, string groupName, [NotNullWhen(true)] out ConsumerExecutorDescriptor? matchTopic)
        {
            if (Entries == null)
            {
                throw new ArgumentNullException(nameof(Entries));
            }

            matchTopic = null;

            if (Entries.TryGetValue(groupName, out var groupMatchTopics))
            {
                matchTopic = _selector.SelectBestCandidate(topicName, groupMatchTopics);

                return matchTopic != null;
            }

            return false;
        }
    }
}
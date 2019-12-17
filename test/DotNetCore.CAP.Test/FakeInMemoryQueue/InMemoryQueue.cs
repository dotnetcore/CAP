using System;
using System.Collections.Generic;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Test.FakeInMemoryQueue
{
    internal class InMemoryQueue
    {
        private readonly ILogger<InMemoryQueue> _logger;
        private static readonly object Lock = new object();

        private readonly Dictionary<string, (Action<TransportMessage>, List<string>)> _groupTopics;

        public Dictionary<string, TransportMessage> Messages { get; }

        public InMemoryQueue(ILogger<InMemoryQueue> logger)
        {
            _logger = logger;
            _groupTopics = new Dictionary<string, (Action<TransportMessage>, List<string>)>();
            Messages = new Dictionary<string, TransportMessage>();
        }

        public void Subscribe(string groupId, Action<TransportMessage> received, string topic)
        {
            lock (Lock)
            {
                if (_groupTopics.ContainsKey(groupId))
                {
                    var topics = _groupTopics[groupId];
                    if (!topics.Item2.Contains(topic))
                    {
                        topics.Item2.Add(topic);
                    }
                }
                else
                {
                    _groupTopics.Add(groupId, (received, new List<string> { topic }));
                }
            }
        }

        public void ClearSubscriber()
        {
            _groupTopics.Clear();
        }

        public void Send(string topic, TransportMessage message)
        {
            Messages.Add(topic, message);
            foreach (var groupTopic in _groupTopics)
            {
                if (groupTopic.Value.Item2.Contains(topic))
                {
                    try
                    {
                        groupTopic.Value.Item1?.Invoke(message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Consumption message raises an exception. Group-->{groupTopic.Key} Name-->{topic}");
                    }
                }
            }
        }
    }
}

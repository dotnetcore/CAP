using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Cap <see cref="ICapPublisher"/> default implement.
    /// </summary>
    public class DefaultCapPublisher : ICapPublisher
    {
        private readonly ICapMessageStore _store;
        private readonly ILogger _logger;

        public DefaultCapPublisher(
            ICapMessageStore store,
            ILogger<DefaultCapPublisher> logger)
        {
            _store = store;
            _logger = logger;
        }

        public Task PublishAsync(string topic, string content)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (content == null) throw new ArgumentNullException(nameof(content));

            return StoreMessage(topic, content);
        }

        public Task PublishAsync<T>(string topic, T contentObj)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            var content = Helper.ToJson(contentObj);
            if (content == null)
                throw new InvalidCastException(nameof(contentObj));

            return StoreMessage(topic, content);
        }

        private async Task StoreMessage(string topic, string content)
        {
            var message = new CapSentMessage
            {
                KeyName = topic,
                Content = content,
                StatusName = StatusName.Enqueued
            };

            await _store.StoreSentMessageAsync(message);

            WaitHandleEx.PulseEvent.Set();

            _logger.EnqueuingSentMessage(topic, content);
        }
    }
}
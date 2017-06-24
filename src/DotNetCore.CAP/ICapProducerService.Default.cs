using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Cap <see cref="ICapProducerService"/> default implement.
    /// </summary>
    public class DefaultProducerService : ICapProducerService
    {
        private readonly ICapMessageStore _store;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;

        public DefaultProducerService(
            ICapMessageStore store,
            ILogger<DefaultProducerService> logger)
        {
            _store = store;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }

        public Task SendAsync(string topic, string content)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (content == null) throw new ArgumentNullException(nameof(content));

            return StoreMessage(topic, content);
        }

        public Task SendAsync<T>(string topic, T contentObj)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            var content = Helper.ToJson(contentObj);
            if (content == null)
                throw new InvalidCastException(nameof(contentObj));

            return StoreMessage(topic, content);
        }

        private async Task StoreMessage(string topic, string content)
        {
            var message = new ConsistencyMessage
            {
                Topic = topic,
                Payload = content
            };

            await _store.CreateAsync(message, _cts.Token);

            WaitHandleEx.PulseEvent.Set();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Enqueuing a topic to be store. topic:{topic}, content:{content}", topic, content);
            }
        }
    }
}
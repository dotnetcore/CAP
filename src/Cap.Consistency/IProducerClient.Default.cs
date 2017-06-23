using System;
using System.Threading;
using System.Threading.Tasks;
using Cap.Consistency.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency
{
    public class DefaultProducerClient : IProducerClient
    {
        private readonly IConsistencyMessageStore _store;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;

        public DefaultProducerClient(
            IConsistencyMessageStore store,
            ILogger<DefaultProducerClient> logger) {

            _store = store;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }

        public Task SendAsync(string topic, string content) {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (content == null) throw new ArgumentNullException(nameof(content));

            return StoreMessage(topic, content);
        }

        public Task SendAsync<T>(string topic, T obj) {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            var content = Helper.ToJson(obj);
            if (content == null)
                throw new InvalidCastException(nameof(obj));

            return StoreMessage(topic, content);
        }

        private async Task StoreMessage(string topic, string content) {

            var message = new ConsistencyMessage {
                Topic = topic,
                Payload = content
            };

            await _store.CreateAsync(message, _cts.Token);

            WaitHandleEx.PulseEvent.Set();

            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug("Enqueuing a topic to be store. topic:{topic}, content:{content}", topic, content);
            }
        }
    }
}

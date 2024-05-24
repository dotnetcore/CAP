using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Test.FakeInMemoryQueue
{
    internal sealed class InMemoryConsumerClient : IConsumerClient
    {
        private readonly ILogger _logger;
        private readonly InMemoryQueue _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly byte _concurrent;
        private readonly string _subscriptionName;

        public InMemoryConsumerClient(ILogger logger, InMemoryQueue queue, string subscriptionName, byte concurrent)
        {
            _logger = logger;
            _queue = queue;
            _concurrent = concurrent;
            _semaphore = new SemaphoreSlim(_concurrent);
            _subscriptionName = subscriptionName;
        }

        public Func<TransportMessage, object, Task> OnMessageCallback { get; set; }

        public Action<LogMessageEventArgs> OnLogCallback { get; set; }

        public BrokerAddress BrokerAddress => new BrokerAddress("InMemory", string.Empty);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null) throw new ArgumentNullException(nameof(topics));

            foreach (var topic in topics)
            {
                _queue.Subscribe(_subscriptionName, OnConsumerReceived, topic);

                _logger.LogInformation($"InMemory message queue initialize the topic: {topic}");
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
        }

        public void Commit(object sender)
        {
            _semaphore.Release();
        }

        public void Reject(object sender)
        {
            _semaphore.Release();
        }

        public void Dispose()
        {
            _queue.ClearSubscriber();
        }

        #region private methods

        private void OnConsumerReceived(TransportMessage e)
        {
            var headers = e.Headers;
            headers.TryAdd(Headers.Group, _subscriptionName);
            if (_concurrent > 0)
            {
                _semaphore.Wait();
                Task.Run(() => OnMessageCallback(e, null)).ConfigureAwait(false);
            }
            else
            {
                OnMessageCallback(e, null).GetAwaiter().GetResult();
            }
        }
        #endregion private methods
    }
}
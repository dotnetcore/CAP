// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Processor
{
    public class Dispatcher : IDispatcher, IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ISubscriberExecutor _executor;
        private readonly ILogger<Dispatcher> _logger;

        private readonly BlockingCollection<CapPublishedMessage> _publishedMessageQueue =
            new BlockingCollection<CapPublishedMessage>(new ConcurrentQueue<CapPublishedMessage>());

        private readonly BlockingCollection<CapReceivedMessage> _receivedMessageQueue =
            new BlockingCollection<CapReceivedMessage>(new ConcurrentQueue<CapReceivedMessage>());

        private readonly IPublishMessageSender _sender;

        public Dispatcher(ILogger<Dispatcher> logger,
            IPublishMessageSender sender,
            ISubscriberExecutor executor)
        {
            _logger = logger;
            _sender = sender;
            _executor = executor;

            Task.Factory.StartNew(Sending, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Task.Factory.StartNew(Processing, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void EnqueueToPublish(CapPublishedMessage message)
        {
            _publishedMessageQueue.Add(message);
        }

        public void EnqueueToExecute(CapReceivedMessage message)
        {
            _receivedMessageQueue.Add(message);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }

        private void Sending()
        {
            try
            {
                while (!_publishedMessageQueue.IsCompleted)
                {
                    if (_publishedMessageQueue.TryTake(out var message, 3000, _cts.Token))
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await _sender.SendAsync(message);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"An exception occurred when sending a message to the MQ. Topic:{message.Name}, Id:{message.Id}");
                            }
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        private void Processing()
        {
            try
            {
                foreach (var message in _receivedMessageQueue.GetConsumingEnumerable(_cts.Token))
                {
                    _executor.ExecuteAsync(message, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }
    }
}
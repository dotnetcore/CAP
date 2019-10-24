// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Processor
{
    public class Dispatcher : IDispatcher, IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IMessageSender _sender;
        private readonly ISubscriberExecutor _executor;
        private readonly ILogger<Dispatcher> _logger;

        private readonly BlockingCollection<MediumMessage> _publishedMessageQueue =
            new BlockingCollection<MediumMessage>(new ConcurrentQueue<MediumMessage>());

        private readonly BlockingCollection<(MediumMessage, ConsumerExecutorDescriptor)> _receivedMessageQueue =
            new BlockingCollection<(MediumMessage, ConsumerExecutorDescriptor)>(new ConcurrentQueue<(MediumMessage, ConsumerExecutorDescriptor)>());

        public Dispatcher(ILogger<Dispatcher> logger,
            IMessageSender sender,
            ISubscriberExecutor executor)
        {
            _logger = logger;
            _sender = sender;
            _executor = executor;

            Task.Factory.StartNew(Sending, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Task.Factory.StartNew(Processing, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void EnqueueToPublish(MediumMessage message)
        {
            _publishedMessageQueue.Add(message);
        }

        public void EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor)
        {
            _receivedMessageQueue.Add((message, descriptor));
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
                                _logger.LogError(ex, $"An exception occurred when sending a message to the MQ. Id:{message.DbId}");
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
                    _executor.ExecuteAsync(message.Item1, message.Item2, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }
    }
}
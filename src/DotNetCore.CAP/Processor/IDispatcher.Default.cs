// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Processor
{
    public class Dispatcher : IDispatcher, IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IMessageSender _sender;
        private readonly ISubscribeDispatcher _executor;
        private readonly ILogger<Dispatcher> _logger;

        private readonly BlockingCollection<MediumMessage> _publishedMessageQueue =
            new BlockingCollection<MediumMessage>(new ConcurrentQueue<MediumMessage>());

        private readonly BlockingCollection<(MediumMessage, ConsumerExecutorDescriptor)> _receivedMessageQueue =
            new BlockingCollection<(MediumMessage, ConsumerExecutorDescriptor)>(new ConcurrentQueue<(MediumMessage, ConsumerExecutorDescriptor)>());

        public Dispatcher(ILogger<Dispatcher> logger,
            IMessageSender sender,
            ISubscribeDispatcher executor)
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

        private async Task Sending()
        {
            try
            {
                while (!_publishedMessageQueue.IsCompleted)
                {
                    if (_publishedMessageQueue.TryTake(out var message, 3000, _cts.Token))
                    {
                        try
                        {
                            var result = await _sender.SendAsync(message);
                            if (!result.Succeeded)
                            {
                                _logger.MessagePublishException(message.Origin.GetId(), result.ToString(), result.Exception);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred when sending a message to the MQ. Id:{message.DbId}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        private async Task Processing()
        {
            try
            {
                foreach (var message in _receivedMessageQueue.GetConsumingEnumerable(_cts.Token))
                {
                    await _executor.DispatchAsync(message.Item1, message.Item2, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }
    }
}
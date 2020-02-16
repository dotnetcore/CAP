// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class Dispatcher : IDispatcher, IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IMessageSender _sender;
        private readonly ISubscribeDispatcher _executor;
        private readonly ILogger<Dispatcher> _logger;

        private readonly Channel<MediumMessage> _publishedChannel;
        private readonly Channel<(MediumMessage, ConsumerExecutorDescriptor)> _receivedChannel;

        public Dispatcher(ILogger<Dispatcher> logger,
            IMessageSender sender,
            IOptions<CapOptions> options,
            ISubscribeDispatcher executor)
        {
            _logger = logger;
            _sender = sender;
            _executor = executor;

            _publishedChannel = Channel.CreateUnbounded<MediumMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
            _receivedChannel = Channel.CreateUnbounded<(MediumMessage, ConsumerExecutorDescriptor)>();

            Task.Factory.StartNew(Sending, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Task.WhenAll(Enumerable.Range(0, options.Value.ConsumerThreadCount)
                .Select(_ => Task.Factory.StartNew(Processing, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());
        }

        public void EnqueueToPublish(MediumMessage message)
        {
            _publishedChannel.Writer.TryWrite(message);
        }

        public void EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor)
        {
            _receivedChannel.Writer.TryWrite((message, descriptor));
        }

        public void Dispose()
        {
            _cts.Cancel();
        }

        private async Task Sending()
        {
            try
            {
                while (await _publishedChannel.Reader.WaitToReadAsync(_cts.Token))
                {
                    while (_publishedChannel.Reader.TryRead(out var message))
                    {
                        try
                        {
                            var result = await _sender.SendAsync(message);
                            if (!result.Succeeded)
                            {
                                _logger.MessagePublishException(message.Origin.GetId(), result.ToString(),
                                    result.Exception);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                $"An exception occurred when sending a message to the MQ. Id:{message.DbId}");
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
                while (await _receivedChannel.Reader.WaitToReadAsync(_cts.Token))
                {
                    while (_receivedChannel.Reader.TryRead(out var message))
                    {
                        await _executor.DispatchAsync(message.Item1, message.Item2, _cts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }
    }
}
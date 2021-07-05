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
    public class Dispatcher : IDispatcher
    {
        private readonly IMessageSender _sender;
        private readonly CapOptions _options;
        private readonly ISubscribeDispatcher _executor;
        private readonly ILogger<Dispatcher> _logger;

        private Channel<MediumMessage> _publishedChannel;
        private Channel<(MediumMessage, ConsumerExecutorDescriptor)> _receivedChannel;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public Dispatcher(ILogger<Dispatcher> logger,
            IMessageSender sender,
            IOptions<CapOptions> options,
            ISubscribeDispatcher executor)
        {
            _logger = logger;
            _sender = sender;
            _options = options.Value;
            _executor = executor;
        }

        public void Start(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            stoppingToken.Register(() => _cts.Cancel());

            _publishedChannel = Channel.CreateUnbounded<MediumMessage>(new UnboundedChannelOptions() { SingleReader = false, SingleWriter = true });
            _receivedChannel = Channel.CreateUnbounded<(MediumMessage, ConsumerExecutorDescriptor)>();

            Task.WhenAll(Enumerable.Range(0, _options.ProducerThreadCount)
                .Select(_ => Task.Factory.StartNew(() => Sending(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

            Task.WhenAll(Enumerable.Range(0, _options.ConsumerThreadCount)
                .Select(_ => Task.Factory.StartNew(() => Processing(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());
        }

        public void EnqueueToPublish(MediumMessage message)
        {
            _publishedChannel.Writer.TryWrite(message);
        }

        public void EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor)
        {
            _receivedChannel.Writer.TryWrite((message, descriptor));
        }

        private async Task Sending(CancellationToken cancellationToken)
        {
            try
            {
                while (await _publishedChannel.Reader.WaitToReadAsync(cancellationToken))
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

        private async Task Processing(CancellationToken cancellationToken)
        {
            try
            {
                while (await _receivedChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (_receivedChannel.Reader.TryRead(out var message))
                    {
                        try
                        {
                            await _executor.DispatchAsync(message.Item1, message.Item2, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            //expected
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e,
                                $"An exception occurred when invoke subscriber. MessageId:{message.Item1.DbId}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        public void Dispose()
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();
        }
    }
}
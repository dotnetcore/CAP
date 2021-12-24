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
        private readonly CancellationTokenSource _cts = new ();

        private Channel<MediumMessage> _publishedChannel = default!;
        private Channel<(MediumMessage, ConsumerExecutorDescriptor)> _receivedChannel = default!;

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

            var capacity = _options.ProducerThreadCount * 500;
            _publishedChannel = Channel.CreateBounded<MediumMessage>(new BoundedChannelOptions(capacity > 5000 ? 5000 : capacity)
            {
                AllowSynchronousContinuations = true,
                SingleReader = _options.ProducerThreadCount == 1,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            capacity = _options.ConsumerThreadCount * 300;
            _receivedChannel = Channel.CreateBounded<(MediumMessage, ConsumerExecutorDescriptor)>(new BoundedChannelOptions(capacity > 3000 ? 3000 : capacity)
            {
                AllowSynchronousContinuations = true,
                SingleReader = _options.ConsumerThreadCount == 1,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });


            Task.WhenAll(Enumerable.Range(0, _options.ProducerThreadCount)
                .Select(_ => Task.Factory.StartNew(() => Sending(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

            Task.WhenAll(Enumerable.Range(0, _options.ConsumerThreadCount)
                .Select(_ => Task.Factory.StartNew(() => Processing(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

            _logger.LogInformation("Starting default Dispatcher");
        }

        public void EnqueueToPublish(MediumMessage message)
        {
            try
            {
                if (!_publishedChannel.Writer.TryWrite(message))
                {
                    while (_publishedChannel.Writer.WaitToWriteAsync(_cts.Token).AsTask().ConfigureAwait(false).GetAwaiter().GetResult())
                    {
                        if (_publishedChannel.Writer.TryWrite(message))
                        {
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Ignore
            }
        }

        public void EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor)
        {
            try
            {
                if (!_receivedChannel.Writer.TryWrite((message, descriptor)))
                {
                    while (_receivedChannel.Writer.WaitToWriteAsync(_cts.Token).AsTask().ConfigureAwait(false).GetAwaiter().GetResult())
                    {
                        if (_receivedChannel.Writer.TryWrite((message, descriptor)))
                        {
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Ignore
            }
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
                                _logger.MessagePublishException(message.Origin?.GetId(), result.ToString(),
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
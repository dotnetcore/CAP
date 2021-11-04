// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Processor
{
    public class Dispatcher : IDispatcher
    {
        private readonly IMessageSender _sender;
        private readonly IServiceProvider _sp;
        private readonly CapOptions _options;
        private readonly ISubscribeDispatcher _executor;
        private readonly ILogger<Dispatcher> _logger;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private Channel<MediumMessage> _publishedChannel;
        private ConcurrentDictionary<string, Channel<(MediumMessage, ConsumerExecutorDescriptor)>> _receivedChannel;

        public Dispatcher(ILogger<Dispatcher> logger,
            IMessageSender sender,
            IOptions<CapOptions> options,
            IServiceProvider serviceProvider,
            ISubscribeDispatcher executor)
        {
            _logger = logger;
            _sender = sender;
            _sp = serviceProvider;
            _options = options.Value;
            _executor = executor;
            _receivedChannel = new ConcurrentDictionary<string, Channel<(MediumMessage, ConsumerExecutorDescriptor)>>();
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


            var selector = _sp.GetService<MethodMatcherCache>();

            foreach (var item in selector.GetCandidatesMethodsOfGroupNameGrouped())
            {
                var key = item.Key.Replace("." + _options.Version, "");
                if (!_options.ConsumerGroupThreadCount.TryGetValue(key, out int threadCount))
                {
                    threadCount = 1;
                }

                capacity = threadCount * 300;

                _receivedChannel.TryAdd(item.Key, Channel.CreateBounded<(MediumMessage, ConsumerExecutorDescriptor)>(
                    new BoundedChannelOptions(capacity > 3000 ? 3000 : capacity)
                    {
                        AllowSynchronousContinuations = true,
                        SingleReader = threadCount == 1,
                        SingleWriter = true,
                        FullMode = BoundedChannelFullMode.Wait
                    }));

                Task.WhenAll(Enumerable.Range(0, threadCount)
                    .Select(_ => Task.Factory.StartNew(
                        () => Processing(item.Key, stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                    ).ToArray()
                );
            }

            Task.WhenAll(Enumerable.Range(0, _options.ProducerThreadCount)
                .Select(_ => Task.Factory.StartNew(() => Sending(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

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
                var channel = _receivedChannel[message.Origin.GetGroup()];
                if (!channel.Writer.TryWrite((message, descriptor)))
                {
                    while (channel.Writer.WaitToWriteAsync(_cts.Token).AsTask().ConfigureAwait(false).GetAwaiter().GetResult())
                    {
                        if (channel.Writer.TryWrite((message, descriptor)))
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

        private async Task Processing(string group, CancellationToken cancellationToken)
        {
            try
            {
                while (await _receivedChannel[group].Reader.WaitToReadAsync(cancellationToken))
                {
                    while (_receivedChannel[group].Reader.TryRead(out var message))
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
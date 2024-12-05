// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.JetStream;

namespace DotNetCore.CAP.NATS;

internal sealed class NATSConsumerClient : IConsumerClient
{
    private static readonly object ConnectionLock = new();

    private readonly string _groupName;
    private readonly byte _groupConcurrent;
    private readonly IServiceProvider _serviceProvider;
    private readonly NATSOptions _natsOptions;
    private readonly SemaphoreSlim _semaphore;
    private IConnection? _consumerClient;

    public NATSConsumerClient(string groupName, byte groupConcurrent, IOptions<NATSOptions> options, IServiceProvider serviceProvider)
    {
        _groupName = groupName;
        _groupConcurrent = groupConcurrent;
        _serviceProvider = serviceProvider;
        _semaphore = new SemaphoreSlim(groupConcurrent);
        _natsOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

    public BrokerAddress BrokerAddress => new("NATS", _natsOptions.Servers);

    public ICollection<string> FetchTopics(IEnumerable<string> topicNames)
    {
        if (_natsOptions.EnableSubscriberClientStreamAndSubjectCreation)
        {
            Connect();

            var jsm = _consumerClient!.CreateJetStreamManagementContext();

            var streamSubjectsGroups = topicNames.GroupBy(x => _natsOptions.NormalizeStreamName(x));

            foreach (var streamSubjectsGroup in streamSubjectsGroups)
            {
                var builder = StreamConfiguration.Builder()
                    .WithName(streamSubjectsGroup.Key)
                    .WithNoAck(false)
                    .WithStorageType(StorageType.Memory)
                    .WithSubjects(streamSubjectsGroup.ToList());

                _natsOptions.StreamOptions?.Invoke(builder);

                try
                {
                    jsm.GetStreamInfo(streamSubjectsGroup.Key); // this throws if the stream does not exist

                    jsm.UpdateStream(builder.Build());
                }
                catch (NATSJetStreamException)
                {
                    try
                    {
                        jsm.AddStream(builder.Build());
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        return topicNames.ToList();
    }

    public void Subscribe(IEnumerable<string> topics)
    {
        if (topics == null)
        {
            throw new ArgumentNullException(nameof(topics));
        }

        Connect();

        var js = _consumerClient!.CreateJetStreamContext();
        var streamGroup = topics.GroupBy(x => _natsOptions.NormalizeStreamName(x));

        lock (ConnectionLock)
        {
            foreach (var subjectStream in streamGroup)
            {
                var groupName = Helper.Normalized(_groupName);

                foreach (var subject in subjectStream)
                {
                    try
                    {
                        var consumerConfig = ConsumerConfiguration.Builder()
                                  .WithDurable(Helper.Normalized(groupName + "-" + subject))
                                  .WithDeliverPolicy(DeliverPolicy.New)
                                  .WithAckWait(30000)
                                  .WithAckPolicy(AckPolicy.Explicit);

                        _natsOptions.ConsumerOptions?.Invoke(consumerConfig);

                        var pso = PushSubscribeOptions.Builder()
                            .WithStream(subjectStream.Key)
                            .WithConfiguration(consumerConfig.Build())
                            .Build();

                        js.PushSubscribeAsync(subject, groupName, SubscriptionMessageHandler, false, pso);
                    }
                    catch (Exception e)
                    {
                        OnLogCallback!(new LogMessageEventArgs()
                        {
                            LogType = MqLogType.ConnectError,
                            Reason = $"An error was encountered when attempting to subscribe to subject: {subject}.{Environment.NewLine}" +
                            $"{e.Message}"
                        });
                    }
                }
            }
        }
    }

    public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.WaitHandle.WaitOne(timeout);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private void SubscriptionMessageHandler(object? sender, MsgHandlerEventArgs e)
    {
        if (_groupConcurrent > 0)
        {
            _semaphore.Wait();
            Task.Run(() => Consume()).ConfigureAwait(false);
        }
        else
        {
            Consume().GetAwaiter().GetResult();
        }

        Task Consume()
        {
            var headers = new Dictionary<string, string?>();

            foreach (string h in e.Message.Header.Keys)
            {
                headers.Add(h, e.Message.Header[h]);
            }

            headers[Headers.Group] = _groupName;

            if (_natsOptions.CustomHeadersBuilder != null)
            {
                var customHeaders = _natsOptions.CustomHeadersBuilder(e, _serviceProvider);
                foreach (var customHeader in customHeaders)
                {
                    headers[customHeader.Key] = customHeader.Value;
                }
            }

            return OnMessageCallback!(new TransportMessage(headers, e.Message.Data), e.Message);
        }
    }

    public void Commit(object? sender)
    {
        if (sender is Msg msg)
        {
            msg.Ack();
        }
        _semaphore.Release();
    }

    public void Reject(object? sender)
    {
        if (sender is Msg msg)
        {
            msg.Nak();
        }
        _semaphore.Release();
    }

    public void Dispose()
    {
        _consumerClient?.Dispose();
    }

    public void Connect()
    {
        if (_consumerClient != null)
        {
            return;
        }

        lock (ConnectionLock)
        {
            if (_consumerClient == null)
            {
                var opts = _natsOptions.Options ?? ConnectionFactory.GetDefaultOptions();
                opts.Url ??= _natsOptions.Servers;
                opts.DisconnectedEventHandler = DisconnectedEventHandler;
                opts.AsyncErrorEventHandler = AsyncErrorEventHandler;
                opts.Timeout = 5000;
                opts.AllowReconnect = false;
                opts.NoEcho = true;

                _consumerClient = new ConnectionFactory().CreateConnection(opts);
            }
        }
    }

    private void DisconnectedEventHandler(object? sender, ConnEventArgs e)
    {
        if (e.Error is null) return;

        var logArgs = new LogMessageEventArgs
        {
            LogType = MqLogType.ConnectError,
            Reason = e.Error.ToString()
        };
        OnLogCallback!(logArgs);
    }

    private void AsyncErrorEventHandler(object? sender, ErrEventArgs e)
    {
        var logArgs = new LogMessageEventArgs
        {
            LogType = MqLogType.AsyncErrorEvent,
            Reason = e.Error
        };
        OnLogCallback!(logArgs);
    }
}
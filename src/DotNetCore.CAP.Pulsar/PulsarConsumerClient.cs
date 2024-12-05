// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using Pulsar.Client.Api;
using Pulsar.Client.Common;

namespace DotNetCore.CAP.Pulsar;

internal sealed class PulsarConsumerClient : IConsumerClient
{
    private readonly PulsarClient _client;
    private readonly string _groupId;
    private readonly byte _groupConcurrent;
    private readonly SemaphoreSlim _semaphore;
    private readonly PulsarOptions _pulsarOptions;
    private IConsumer<byte[]>? _consumerClient;

    public PulsarConsumerClient(IOptions<PulsarOptions> options, PulsarClient client, string groupName, byte groupConcurrent)
    {
        _client = client;
        _groupId = groupName;
        _groupConcurrent = groupConcurrent;
        _semaphore = new SemaphoreSlim(groupConcurrent);
        _pulsarOptions = options.Value;
    }

    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

    public BrokerAddress BrokerAddress => new("Pulsar", _pulsarOptions.ServiceUrl);

    public void Subscribe(IEnumerable<string> topics)
    {
        if (topics == null) throw new ArgumentNullException(nameof(topics));

        var serviceName = Assembly.GetEntryAssembly()?.GetName().Name!.ToLower();

        _consumerClient = _client.NewConsumer()
            .Topics(topics)
            .SubscriptionName(_groupId)
            .ConsumerName(serviceName)
            .SubscriptionType(SubscriptionType.Shared)
            .SubscribeAsync().GetAwaiter().GetResult();
    }

    public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumerResult = _consumerClient!.ReceiveAsync(cancellationToken).GetAwaiter().GetResult();

                if (_groupConcurrent > 0)
                {
                    _semaphore.Wait(cancellationToken);
                    Task.Run(Consume, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Consume().GetAwaiter().GetResult();
                }

                Task Consume()
                {
                    var headers = new Dictionary<string, string?>(consumerResult.Properties.Count);
                    foreach (var header in consumerResult.Properties)
                    {
                        headers.Add(header.Key, header.Value);
                    }

                    headers[Headers.Group] = _groupId;

                    var message = new TransportMessage(headers, consumerResult.Data);

                    return OnMessageCallback!(message, consumerResult.MessageId);
                }
            }
            catch (Exception e)
            {
                OnLogCallback!(new LogMessageEventArgs
                {
                    LogType = MqLogType.ConsumeError,
                    Reason = e.Message
                });
            }
        }
    }

    public void Commit(object? sender)
    {
        _consumerClient!.AcknowledgeAsync((MessageId)sender!);
        _semaphore.Release();
    }

    public void Reject(object? sender)
    {
        if (sender is MessageId id) _consumerClient!.NegativeAcknowledge(id);
        _semaphore.Release();
    }

    public void Dispose()
    {
        _consumerClient?.DisposeAsync();
    }
}
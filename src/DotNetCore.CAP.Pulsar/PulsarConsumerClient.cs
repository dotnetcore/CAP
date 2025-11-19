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

    public BrokerAddress BrokerAddress => new("pulsar", _pulsarOptions.ServiceUrl);

    public async Task SubscribeAsync(IEnumerable<string> topics)
    {
        if (topics == null) throw new ArgumentNullException(nameof(topics));

        var serviceName = Assembly.GetEntryAssembly()?.GetName().Name!.ToLower();

        _consumerClient = await _client.NewConsumer()
            .Topics(topics)
            .SubscriptionName(_groupId)
            .ConsumerName(serviceName)
            .SubscriptionType(SubscriptionType.Shared)
            .SubscribeAsync();
    }

    public async Task ListeningAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumerResult = await _consumerClient!.ReceiveAsync(cancellationToken);

                if (_groupConcurrent > 0)
                {
                    _semaphore.Wait(cancellationToken);
                    _ = Task.Run(ConsumeAsync, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ConsumeAsync();
                }

                Task ConsumeAsync()
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

    public async Task CommitAsync(object? sender)
    {
        await _consumerClient!.AcknowledgeAsync((MessageId)sender!);
        _semaphore.Release();
    }

    public async Task RejectAsync(object? sender)
    {
        if (sender is MessageId id) 
           await _consumerClient!.NegativeAcknowledge(id);
        _semaphore.Release();
    }

    public ValueTask DisposeAsync()
    {
        return _consumerClient?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
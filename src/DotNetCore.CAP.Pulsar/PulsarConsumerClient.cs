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

namespace DotNetCore.CAP.Pulsar
{
    internal sealed class PulsarConsumerClient : IConsumerClient
    {
        private readonly PulsarClient _client;
        private readonly string _groupId;
        private readonly PulsarOptions _pulsarOptions;
        private IConsumer<byte[]>? _consumerClient;

        public PulsarConsumerClient(PulsarClient client, string groupId, IOptions<PulsarOptions> options)
        {
            _client = client;
            _groupId = groupId;
            _pulsarOptions = options.Value;
        }

        public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

        public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

        public BrokerAddress BrokerAddress => new("Pulsar", _pulsarOptions.ServiceUrl);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

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

                    var headers = new Dictionary<string, string?>(consumerResult.Properties.Count);
                    foreach (var header in consumerResult.Properties)
                    {
                        headers.Add(header.Key, header.Value);
                    }
                    headers.Add(Headers.Group, _groupId);

                    var message = new TransportMessage(headers, consumerResult.Data);

                    OnMessageCallback!(message, consumerResult.MessageId).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    OnLogCallback!(new LogMessageEventArgs()
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
        }

        public void Reject(object? sender)
        {
            if (sender is MessageId id)
            {
                _consumerClient!.NegativeAcknowledge(id);
            }
        }

        public void Dispose()
        {
            _consumerClient?.DisposeAsync();
        }


    }
}
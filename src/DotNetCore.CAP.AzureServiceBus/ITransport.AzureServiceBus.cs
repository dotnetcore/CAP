// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.AzureServiceBus.Producer;
using Azure.Messaging.ServiceBus;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Messaging.ServiceBus.Administration;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal class AzureServiceBusTransport : ITransport, IServiceBusProducerDescriptorFactory, IDisposable
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly ILogger _logger;
        private readonly AzureServiceBusOptions _asbOptions;
        
        private ServiceBusClient? _client;
        private readonly ConcurrentDictionary<string, ServiceBusSender?> _senders = new();
        
        public AzureServiceBusTransport(
            ILogger<AzureServiceBusTransport> logger,
            IOptions<AzureServiceBusOptions> asbOptions)
        {
            _logger = logger;
            _asbOptions = asbOptions.Value ?? throw new ArgumentNullException(nameof(asbOptions));
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("AzureServiceBus", _asbOptions.ConnectionString);

        /// <summary>
        /// Creates a producer descriptor for the given message. If there's no custom producer configuration for the
        /// message type, one will be created using defaults configured in the AzureServiceBusOptions (e.g. TopicPath).
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <returns></returns>
        public IServiceBusProducerDescriptor CreateProducerForMessage(TransportMessage transportMessage)
            => _asbOptions
                   .CustomProducers
                   .Single(p => p.TopicPath == transportMessage.GetName());
        
        public async Task<OperateResult> SendAsync(TransportMessage transportMessage)
        {
            try
            {
                var producer = CreateProducerForMessage(transportMessage);
                var sender = await GetSenderForProducerAsync(producer);
                
                var message = new ServiceBusMessage(transportMessage.Body.ToArray())
                {
                    MessageId = transportMessage.GetId(),
                    Subject = transportMessage.GetName(),
                    CorrelationId = transportMessage.GetCorrelationId()
                };

                if (transportMessage.Headers.TryGetValue(AzureServiceBusHeaders.SessionId, out var sessionId))
                {
                    message.SessionId = sessionId;
                }

                if (
                    transportMessage.Headers.TryGetValue(AzureServiceBusHeaders.ScheduledEnqueueTimeUtc, out var scheduledEnqueueTimeUtcString)
                    && DateTimeOffset.TryParse(scheduledEnqueueTimeUtcString, out var scheduledEnqueueTimeUtc))
                {
                    message.ScheduledEnqueueTime = scheduledEnqueueTimeUtc.UtcDateTime;
                }

                foreach (var header in transportMessage.Headers)
                {
                    message.ApplicationProperties.Add(header.Key, header.Value);
                }

                
                await sender.SendMessageAsync(message);

                _logger.LogDebug($"Azure Service Bus message [{transportMessage.GetName()}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wrapperEx);
            }
        }


        /// <summary>
        /// Gets the Topic Client for the specified producer. If it does not exist, a new one is created and added to the Topic Client dictionary.
        /// </summary>
        /// <param name="producerDescriptor"></param>
        /// <returns><see cref="ServiceBusSender"/></returns>
        private async Task<ServiceBusSender> GetSenderForProducerAsync(IServiceBusProducerDescriptor producerDescriptor)
        {
            var topicPath = producerDescriptor.TopicPath;

            if (!_senders.TryGetValue(topicPath, out var sender) && sender == null)
            {
                await _connectionLock.WaitAsync();

                try
                {
                    if (!_senders.TryGetValue(topicPath, out sender) && sender == null)
                    {
                        _client ??= _asbOptions.TokenCredential is null
                            ? new ServiceBusClient(_asbOptions.ConnectionString)
                            : new ServiceBusClient(_asbOptions.Namespace, _asbOptions.TokenCredential);


                        var newSender = _client.CreateSender(topicPath);

                        _senders.AddOrUpdate(
                            key: topicPath,
                            addValue: newSender,
                            updateValueFactory: (_, _) => newSender);

                        return newSender;
                    }
                }
                finally
                {
                    _connectionLock.Release();
                }
            }

            _logger.LogTrace("Topic {TopicPath} connection already present as a Publish destination.",
                topicPath);

            return sender;
        }

        public void Dispose()
        {
            _connectionLock.Dispose();

            foreach (var sender in _senders)
            {
                sender.Value?.DisposeAsync().GetAwaiter().GetResult();
            }

            _client?.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
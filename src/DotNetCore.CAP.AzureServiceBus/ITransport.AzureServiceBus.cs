// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using DotNetCore.CAP.AzureServiceBus.Helpers;
using DotNetCore.CAP.AzureServiceBus.Producer;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AzureServiceBus;

internal class AzureServiceBusTransport : ITransport, IServiceBusProducerDescriptorFactory
{
    private readonly IOptions<AzureServiceBusOptions> _asbOptions;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, ServiceBusSender?> _senders = new();

    private ServiceBusClient? _client;

    public AzureServiceBusTransport(
        ILogger<AzureServiceBusTransport> logger,
        IOptions<AzureServiceBusOptions> asbOptions)
    {
        _logger = logger;
        _asbOptions = asbOptions;
    }

    /// <summary>
    /// Creates a producer descriptor for the given message. If there's no custom producer configuration for the
    /// message type, one will be created using defaults configured in the AzureServiceBusOptions (e.g. TopicPath).
    /// </summary>
    /// <param name="transportMessage"></param>
    /// <returns></returns>
    public IServiceBusProducerDescriptor CreateProducerForMessage(TransportMessage transportMessage)
    {
        return _asbOptions.Value
                   .CustomProducers
                   .SingleOrDefault(p => p.MessageTypeName == transportMessage.GetName())
               ??
               new ServiceBusProducerDescriptor(
                   transportMessage.GetName(),
                   _asbOptions.Value.TopicPath);
    }

    public BrokerAddress BrokerAddress => ServiceBusHelpers.GetBrokerAddress(_asbOptions.Value.ConnectionString, _asbOptions.Value.Namespace);

    public async Task<OperateResult> SendAsync(TransportMessage transportMessage)
    {
        try
        {
            var producer = CreateProducerForMessage(transportMessage);
            var sender = GetSenderForProducer(producer);

            var message = new ServiceBusMessage(transportMessage.Body.ToArray())
            {
                MessageId = transportMessage.GetId(),
                Subject = transportMessage.GetName(),
                CorrelationId = transportMessage.GetCorrelationId()
            };

            if (_asbOptions.Value.EnableSessions)
            {
                transportMessage.Headers.TryGetValue(AzureServiceBusHeaders.SessionId, out var sessionId);
                message.SessionId = string.IsNullOrEmpty(sessionId) ? transportMessage.GetId() : sessionId;
            }

            if (
                transportMessage.Headers.TryGetValue(AzureServiceBusHeaders.ScheduledEnqueueTimeUtc,
                    out var scheduledEnqueueTimeUtcString)
                && DateTimeOffset.TryParse(scheduledEnqueueTimeUtcString, out var scheduledEnqueueTimeUtc))
                message.ScheduledEnqueueTime = scheduledEnqueueTimeUtc.UtcDateTime;

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
    /// Gets the Topic Client for the specified producer. If it does not exist, a new one is created and added to the Topic
    /// Client dictionary.
    /// </summary>
    /// <param name="producerDescriptor"></param>
    /// <returns>
    ///     <see cref="ServiceBusSender" />
    /// </returns>
    private ServiceBusSender GetSenderForProducer(IServiceBusProducerDescriptor producerDescriptor)
    {
        if (_senders.TryGetValue(producerDescriptor.TopicPath, out var sender) && sender != null)
        {
            _logger.LogTrace("Topic {TopicPath} connection already present as a Publish destination.",
                producerDescriptor.TopicPath);

            return sender;
        }

        _connectionLock.Wait();

        try
        {
            _client ??= _asbOptions.Value.TokenCredential is null
                ? new ServiceBusClient(_asbOptions.Value.ConnectionString)
                : new ServiceBusClient(_asbOptions.Value.Namespace, _asbOptions.Value.TokenCredential);

            var newSender = _client.CreateSender(producerDescriptor.TopicPath);
            _senders.AddOrUpdate(
                producerDescriptor.TopicPath,
                newSender,
                (_, _) => newSender);

            return newSender;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}
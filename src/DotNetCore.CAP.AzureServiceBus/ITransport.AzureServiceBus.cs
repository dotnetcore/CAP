// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal class AzureServiceBusTransport : ITransport
    {
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        private readonly ILogger _logger;
        private readonly IOptions<AzureServiceBusOptions> _asbOptions;

        private ConcurrentDictionary<string, ITopicClient?> _topicClients = new();

        public AzureServiceBusTransport(
            ILogger<AzureServiceBusTransport> logger,
            IOptions<AzureServiceBusOptions> asbOptions)
        {
            _logger = logger;
            _asbOptions = asbOptions;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("AzureServiceBus", _asbOptions.Value.ConnectionString);

        public async Task<OperateResult> SendAsync(TransportMessage transportMessage)
        {
            var destinationTopicPath =
                transportMessage.Headers.TryGetValue(AzureServiceBusHeaders.DestinationTopicPath, out var destinationHeader)
                    ? destinationHeader
                    : _asbOptions.Value.TopicPath;

            if (string.IsNullOrWhiteSpace(destinationTopicPath)) 
                throw new InvalidOperationException("The destination Topic Path must be set either in the TopicPath property of Azure Service Bus options or via the Destination header of AzureServiceBusHeaders.");
            
            try
            {
                Connect(destinationTopicPath!);

                var message = new Microsoft.Azure.ServiceBus.Message
                {
                    MessageId = transportMessage.GetId(),
                    Body = transportMessage.Body,
                    Label = transportMessage.GetName(),
                    CorrelationId = transportMessage.GetCorrelationId()
                };

                if (_asbOptions.Value.EnableSessions)
                {
                    transportMessage.Headers.TryGetValue(AzureServiceBusHeaders.SessionId, out var sessionId);
                    message.SessionId = string.IsNullOrEmpty(sessionId) ? transportMessage.GetId() : sessionId;
                }

                if (
                    transportMessage.Headers.TryGetValue(AzureServiceBusHeaders.ScheduledEnqueueTimeUtc, out var scheduledEnqueueTimeUtcString)
                    && DateTimeOffset.TryParse(scheduledEnqueueTimeUtcString, out var scheduledEnqueueTimeUtc))
                {
                    message.ScheduledEnqueueTimeUtc = scheduledEnqueueTimeUtc.UtcDateTime;
                }

                foreach (var header in transportMessage.Headers)
                {
                    message.UserProperties.Add(header.Key, header.Value);
                }

                _topicClients.TryGetValue(destinationTopicPath, out var topicClient);

                await topicClient!.SendAsync(message);

                _logger.LogDebug($"Azure Service Bus message [{transportMessage.GetName()}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wrapperEx);
            }
        }

        private void Connect(string topic)
        {
            if (_topicClients.TryGetValue(topic, out var _))
            {
                _logger.LogTrace("Topic {TopicPath} connection already present as a Publish destination.");
                return;
            }

            _connectionLock.Wait();

            try
            {
                if (_topicClients.TryAdd(topic, new TopicClient(BrokerAddress.Endpoint, topic, RetryPolicy.NoRetry)))
                {
                    _logger.LogInformation("Topic {TopicPath} connection successfully added as a Publish destination.");
                }
                else
                {
                    _logger.LogError("Error adding Topic {TopicPath} connection as a Publish destination.");
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.AzureServiceBus.Producer;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Message = Microsoft.Azure.ServiceBus.Message;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal class AzureServiceBusTransport : ITransport, IServiceBusProducerDescriptorFactory
    {
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        private readonly ILogger _logger;
        private readonly IOptions<AzureServiceBusOptions> _asbOptions;
        private readonly ConcurrentDictionary<string, ITopicClient?> _topicClients = new();

        public AzureServiceBusTransport(
            ILogger<AzureServiceBusTransport> logger,
            IOptions<AzureServiceBusOptions> asbOptions)
        {
            _logger = logger;
            _asbOptions = asbOptions;
        }

        public BrokerAddress BrokerAddress => new("AzureServiceBus", _asbOptions.Value.ConnectionString);

        /// <summary>
        /// Gets a custom Producer
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <returns></returns>
        public IServiceBusProducerDescriptor CreateProducerForMessage(TransportMessage transportMessage)
            => _asbOptions.Value
                   .CustomProducers
                   .SingleOrDefault(p => p.MessageTypeName == transportMessage.GetName())
               ??
               new ServiceBusProducerDescriptorDescriptor(
                   typeName: transportMessage.GetName(),
                   topicPath: _asbOptions.Value.TopicPath);
        
        public async Task<OperateResult> SendAsync(TransportMessage transportMessage)
        {
            try
            {
                var producer = CreateProducerForMessage(transportMessage);

                var topicClient = GetTopicClientForProducer(producer);

                var message = new Message
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

                await topicClient.SendAsync(message);

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
        /// <returns><see cref="ITopicClient"/></returns>
        private ITopicClient GetTopicClientForProducer(IServiceBusProducerDescriptor producerDescriptor)
        {
            if (_topicClients.TryGetValue(producerDescriptor.TopicPath, out var topicClient) && topicClient != null)
            {
                _logger.LogTrace("Topic {TopicPath} connection already present as a Publish destination.",
                    producerDescriptor.TopicPath);

                return topicClient;
            }

            _connectionLock.Wait();

            try
            {
                topicClient = new TopicClient(
                    connectionString: BrokerAddress.Endpoint, 
                    entityPath: producerDescriptor.TopicPath);

                _topicClients.AddOrUpdate(
                    key: producerDescriptor.TopicPath,
                    addValue: topicClient,
                    updateValueFactory: (_, _) => topicClient);

                return topicClient;
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}

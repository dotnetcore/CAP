// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal class AzureServiceBusTransport : ITransport
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly ILogger _logger;
        private readonly IOptions<AzureServiceBusOptions> _asbOptions;

        private ServiceBusClient? _client;
        private ServiceBusSender? _sender;

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
            try
            {
                Connect();
                
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
                    transportMessage.Headers.TryGetValue(AzureServiceBusHeaders.ScheduledEnqueueTimeUtc, out var scheduledEnqueueTimeUtcString)
                    && DateTimeOffset.TryParse(scheduledEnqueueTimeUtcString, out var scheduledEnqueueTimeUtc))
                {
                    message.ScheduledEnqueueTime = scheduledEnqueueTimeUtc.UtcDateTime;
                }

                foreach (var header in transportMessage.Headers)
                {
                    message.ApplicationProperties.Add(header.Key, header.Value);
                }

                await _sender!.SendMessageAsync(message);

                _logger.LogDebug($"Azure Service Bus message [{transportMessage.GetName()}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wrapperEx);
            }
        }

        private void Connect()
        {
            if (_client != null)
            {
                return;
            }

            _connectionLock.Wait();

            try
            {
                _client ??= _asbOptions.Value.TokenCredential is null ? new ServiceBusClient(_asbOptions.Value.ConnectionString) :
                                                                         new ServiceBusClient(_asbOptions.Value.Namespace,_asbOptions.Value.TokenCredential);

                _sender ??= _client.CreateSender(_asbOptions.Value.TopicPath);
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}
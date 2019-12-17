// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly ILogger _logger;
        private readonly IOptions<AzureServiceBusOptions> _asbOptions;

        private ITopicClient _topicClient;

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

                var message = new Microsoft.Azure.ServiceBus.Message
                {
                    MessageId = transportMessage.GetId(),
                    Body = transportMessage.Body,
                    Label = transportMessage.GetName()
                };

                foreach (var header in transportMessage.Headers)
                {
                    message.UserProperties.Add(header.Key, header.Value);
                }

                await _topicClient.SendAsync(message);

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
            if (_topicClient != null)
            {
                return;
            }

            _connectionLock.Wait();

            try
            {
                if (_topicClient == null)
                {
                    _topicClient = new TopicClient(BrokerAddress.Endpoint, _asbOptions.Value.TopicPath, RetryPolicy.NoRetry);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}
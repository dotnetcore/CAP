// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.GooglePubSub
{
    internal class GooglePubSubTransport : ITransport
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly ILogger _logger;

        private PublisherServiceApiClient _publisherClient;

        public GooglePubSubTransport(ILogger<GooglePubSubTransport> logger)
        {
            _logger = logger;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("GooglePubSub", string.Empty);

        public async Task<OperateResult> SendAsync(TransportMessage transportMessage)
        {
            try
            {
                Connect();

                var message = new PubsubMessage
                {
                    Data = ByteString.CopyFrom(transportMessage.Body)
                };
                message.Attributes.Add(transportMessage.Headers);

                await _publisherClient.PublishAsync(transportMessage.GetName(), new[] { message });

                _logger.LogDebug($"Topic message [{transportMessage.GetName()}] has been published.");

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
            if (_publisherClient != null)
            {
                return;
            }

            _connectionLock.Wait();

            try
            {
                _publisherClient ??= PublisherServiceApiClient.Create();
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}

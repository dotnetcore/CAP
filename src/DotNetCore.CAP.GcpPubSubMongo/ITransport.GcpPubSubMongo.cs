// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Google.Api.Gax.ResourceNames;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.GooglePubSub
{
    internal class GcpPubSubMongoTransport : ITransport
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly IOptions<GcpPubSubMongoOptions> _options;
        private readonly ILogger _logger;

        private PublisherServiceApiClient _publisherClient;

        public GcpPubSubMongoTransport(IOptions<GcpPubSubMongoOptions> options,
            ILogger<GcpPubSubMongoTransport> logger)
        {
            _options = options;
            _logger = logger;

            Connect();

            CreateTopic();
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("GooglePubSubMongo", string.Empty);

        public async Task<OperateResult> SendAsync(TransportMessage transportMessage)
        {
            try
            {
                Connect();

                var message = new PubsubMessage
                {
                    Data = ByteString.CopyFrom(transportMessage.Body)
                };

                foreach (var header in transportMessage.Headers)
                {
                    if (header.Value != null)
                        message.Attributes.Add(header.Key, header.Value);
                }
                var topicName = new TopicName(_options.Value.ProjectId, _options.Value.TopicId);

                await _publisherClient.PublishAsync(topicName, new[] { message });

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

        private void CreateTopic()
        {
            var credential = GoogleCredential.GetApplicationDefault();

            var topicName = new TopicName(_options.Value.ProjectId, _options.Value.TopicId);
            if (_publisherClient.ListTopics(new ProjectName(_options.Value.ProjectId))
                .All(x => x.TopicName != topicName))
            {
                _publisherClient.CreateTopic(topicName);
            }
        }
    }
}

// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.GooglePubSub
{
    internal sealed class GooglePubSubConsumerClient : IConsumerClient
    {
        private readonly ILogger _logger;
        private readonly string _subscriptionName;
        private readonly ProjectName _projectName;
        private readonly GooglePubSubOptions _googlePubSubOptions;
        private SubscriberServiceApiClient _subscriberClient;

        public GooglePubSubConsumerClient(ILogger logger, string subscriptionName, IOptions<GooglePubSubOptions> options)
        {
            _logger = logger;
            _subscriptionName = subscriptionName;
            _googlePubSubOptions = options.Value;
            _projectName = new ProjectName(_googlePubSubOptions.ProjectId);
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("GooglePubSub", string.Empty);

        public void Subscribe(IEnumerable<string> topics)
        {

            var publisher = PublisherServiceApiClient.Create();
            var gcpPagedTopics = publisher.ListTopics(new ListTopicsRequest()
            {
                ProjectAsProjectName = _projectName,
                PageSize = int.MaxValue
            });

            var gcpTopics = gcpPagedTopics.ReadPage(1)
                .Select(x => x.TopicName)
                .Select(x => x.TopicId).ToList();

            var subscriptions = _subscriberClient.ListSubscriptions(_projectName, pageSize: int.MaxValue);

            subscriptions.ReadPage(1).Select(x=>x.).ToList()

            var subscriptionName = new SubscriptionName(_googlePubSubOptions.ProjectId, _subscriptionName);

            foreach (var topic in topics)
            {
                var topicName = new TopicName(_googlePubSubOptions.ProjectId, topic);
                if (!gcpTopics.Contains(topic))
                {
                    var t = publisher.CreateTopic(topicName);
                    _subscriberClient.CreateSubscription(subscriptionName, t.TopicName, null, 60);
                }
                else
                {
                    _subscriberClient.CreateSubscription(subscriptionName, topicName, null, 60);
                }
            }

            
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            var subscriptionName = new SubscriptionName(_googlePubSubOptions.ProjectId, _subscriptionName);

            while (true)
            {
                PullFromGcp:

                var response = _subscriberClient.Pull(subscriptionName, returnImmediately: true, maxMessages: 1);
                if (response.ReceivedMessages.Count > 0)
                {
                    OnConsumerReceived(response.ReceivedMessages[0]);

                    goto PullFromGcp;
                }
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
            // ReSharper disable once FunctionNeverReturns
        }


        public void Commit(object sender)
        {
            _subscriberClient.Acknowledge(_subscriptionName, new[] { (string)sender });
        }

        public void Reject(object sender)
        {
            // ignore
        }

        public void Dispose()
        {

        }

        public void Connect()
        {
            _subscriberClient ??= SubscriberServiceApiClient.Create();
        }

        #region private methods

        private void OnConsumerReceived(ReceivedMessage message)
        {
            var header = message.Message.Attributes
                .ToDictionary(x => x.Key, y => y.Value);
            header.Add(Headers.Group, _subscriptionName);

            var context = new TransportMessage(header, message.Message.Data.ToByteArray());

            OnMessageReceived?.Invoke(message.AckId, context);
        }

        #endregion private methods
    }
}
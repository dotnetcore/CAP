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
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.GooglePubSub
{
    internal sealed class GcpPubSubMongoConsumerClient : IConsumerClient
    {
        private readonly ProjectName _projectName;
        private readonly SubscriptionName _subscriptionName;
        private readonly TopicName _topicName;
        private SubscriberServiceApiClient _subscriberClient;

        public GcpPubSubMongoConsumerClient(string subscriptionName, IOptions<GcpPubSubMongoOptions> options)
        {
            _projectName = new ProjectName(options.Value.ProjectId);
            _subscriptionName = new SubscriptionName(options.Value.ProjectId, subscriptionName);
            _topicName = new TopicName(options.Value.ProjectId, options.Value.TopicId);
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("GooglePubSub", string.Empty);

        public void Subscribe(IEnumerable<string> topics)
        {
            var hasSubscriptions = _subscriberClient.ListSubscriptions(_projectName)
                .Any(x => x.TopicAsTopicName == _topicName && x.SubscriptionName == _subscriptionName);

            if (!hasSubscriptions)
            {
                _subscriberClient.CreateSubscription(_subscriptionName, _topicName, null, 60);
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();
            
            while (true)
            {
                PullFromGcp:

                var response = _subscriberClient.Pull(_subscriptionName, returnImmediately: true, maxMessages: 1);
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
            header.Add(Headers.Group, _subscriptionName.SubscriptionId);

            //TODO: Since GCP does not support multiple topics attached to one subscription,
            //TODO: multiple messages will be generated here and need to be removed
            var context = new TransportMessage(header, message.Message.Data.ToByteArray());

            OnMessageReceived?.Invoke(message.AckId, context);
        }

        #endregion private methods
    }
}
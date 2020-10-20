// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.GooglePubSub
{
    internal sealed class GooglePubSubConsumerClient : IConsumerClient
    {
        private readonly ILogger _logger;
        private readonly string _subscriptionName;
        private SubscriberServiceApiClient _subscriberClient;

        public GooglePubSubConsumerClient(ILogger logger, string subscriptionName)
        {
            _logger = logger;
            _subscriptionName = subscriptionName;
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("GooglePubSub", string.Empty);

        public void Subscribe(IEnumerable<string> topics)
        {
            var publisher = PublisherServiceApiClient.Create();
            //var gcpTopics = publisher.ListTopics(new ListTopicsRequest()
            //{
            //    Project = "",
            //    PageSize = int.MaxValue
            //});

            //gcpTopics.ReadPage(int.MaxValue);

            foreach (var topic in topics)
            {
                publisher.CreateTopic(new TopicName("projectId", topic));
            }

        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            var subscriptionName = new SubscriptionName("projectId", _subscriptionName);

            while (true)
            {
                var response = _subscriberClient.Pull(subscriptionName, returnImmediately: true, maxMessages: 1);
                if (response.ReceivedMessages.Count > 0)
                {
                    OnConsumerReceived(response.ReceivedMessages[0]);
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

        //private Task OnExceptionReceived(ExceptionReceivedEventArgs args)
        //{
        //    var context = args.ExceptionReceivedContext;
        //    var exceptionMessage =
        //        $"- Endpoint: {context.Endpoint}" + Environment.NewLine +
        //        $"- Entity Path: {context.EntityPath}" + Environment.NewLine +
        //        $"- Executing Action: {context.Action}" + Environment.NewLine +
        //        $"- Exception: {args.Exception}";

        //    var logArgs = new LogMessageEventArgs
        //    {
        //        LogType = MqLogType.ExceptionReceived,
        //        Reason = exceptionMessage
        //    };

        //    OnLog?.Invoke(null, logArgs);

        //    return Task.CompletedTask;
        //}

        #endregion private methods
    }
}
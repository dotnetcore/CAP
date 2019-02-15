// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal sealed class AzureServiceBusConsumerClient : IConsumerClient
    {
        private readonly string _groupId;
        private readonly IConnectionPool _connectionPool;
        private readonly AzureServiceBusOptions _asbOptions;

        private SubscriptionClient _consumerClient;
        private string _lockToken;

        public AzureServiceBusConsumerClient(string groupId,
            IConnectionPool connectionPool,
            AzureServiceBusOptions options)
        {
            _groupId = groupId;
            _connectionPool = connectionPool;
            _asbOptions = options ?? throw new ArgumentNullException(nameof(options));

            InitAzureServiceBusClient();
        }

        public event EventHandler<MessageContext> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public string ServersAddress => _asbOptions.ConnectionString;

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            _consumerClient.RemoveRuleAsync(RuleDescription.DefaultRuleName).Wait();

            foreach (var topic in topics)
            {
                _consumerClient.AddRuleAsync(new RuleDescription
                {
                    Filter = new CorrelationFilter { Label = topic },
                    Name = topic
                }).GetAwaiter().GetResult();
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            _consumerClient.RegisterMessageHandler(OnConsumerReceived, OnExceptionReceived);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit()
        {
            _consumerClient.CompleteAsync(_lockToken);
        }

        public void Reject()
        {
            // _consumerClient.Assign(_consumerClient.Assignment);
        }

        public void Dispose()
        {
            _consumerClient.CloseAsync().Wait();
        }

        #region private methods

        private void InitAzureServiceBusClient()
        {
            _consumerClient = new SubscriptionClient(_connectionPool.Rent(),
                _asbOptions.TopicPath,
                _groupId,
                ReceiveMode.ReceiveAndDelete,
                RetryPolicy.Default);
        }

        private Task OnConsumerReceived(Message message, CancellationToken token)
        {
            _lockToken = message.SystemProperties.LockToken;
            var context = new MessageContext
            {
                Group = _groupId,
                Name = message.Label,
                Content = Encoding.UTF8.GetString(message.Body)
            };

            OnMessageReceived?.Invoke(null, context);

            return Task.CompletedTask;
        }

        private Task OnExceptionReceived(ExceptionReceivedEventArgs args)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ServerConnError,
                Reason = args.Exception.Message
            };

            OnLog?.Invoke(null, logArgs);

            return Task.CompletedTask;
        }

        #endregion private methods
    }
}
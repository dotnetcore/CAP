// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Message = Microsoft.Azure.ServiceBus.Message;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal sealed class AzureServiceBusConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly ILogger _logger;
        private readonly string _subscriptionName;
        private readonly AzureServiceBusOptions _asbOptions;

        private SubscriptionClient _consumerClient;

        public AzureServiceBusConsumerClient(
            ILogger logger,
            string subscriptionName,
            IOptions<AzureServiceBusOptions> options)
        {
            _logger = logger;
            _subscriptionName = subscriptionName;
            _asbOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("AzureServiceBus", _asbOptions.ConnectionString);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            ConnectAsync().GetAwaiter().GetResult();

            var allRuleNames = _consumerClient.GetRulesAsync().GetAwaiter().GetResult().Select(x => x.Name);

            foreach (var newRule in topics.Except(allRuleNames))
            {
                CheckValidSubscriptionName(newRule);

                _consumerClient.AddRuleAsync(new RuleDescription
                {
                    Filter = new CorrelationFilter { Label = newRule },
                    Name = newRule
                }).GetAwaiter().GetResult();

                _logger.LogInformation($"Azure Service Bus add rule: {newRule}");
            }

            foreach (var oldRule in allRuleNames.Except(topics))
            {
                _consumerClient.RemoveRuleAsync(oldRule).GetAwaiter().GetResult();

                _logger.LogInformation($"Azure Service Bus remove rule: {oldRule}");
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConnectAsync().GetAwaiter().GetResult();

            _consumerClient.RegisterMessageHandler(OnConsumerReceived,
                new MessageHandlerOptions(OnExceptionReceived)
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = 10,
                    MaxAutoRenewDuration = TimeSpan.FromSeconds(30)
                });

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit(object sender)
        {
            _consumerClient.CompleteAsync((string)sender);
        }

        public void Reject(object sender)
        {
            // ignore
        }

        public void Dispose()
        {
            _consumerClient?.CloseAsync().Wait(1500);
        }

        public async Task ConnectAsync()
        {
            if (_consumerClient != null)
            {
                return;
            }

            _connectionLock.Wait();

            try
            {
                if (_consumerClient == null)
                {
                    ManagementClient mClient;
                    if (_asbOptions.ManagementTokenProvider != null)
                    {
                        mClient = new ManagementClient(new ServiceBusConnectionStringBuilder(
                            _asbOptions.ConnectionString), _asbOptions.ManagementTokenProvider);
                    }
                    else
                    {
                        mClient = new ManagementClient(_asbOptions.ConnectionString);
                    }

                    if (!await mClient.TopicExistsAsync(_asbOptions.TopicPath))
                    {
                        await mClient.CreateTopicAsync(_asbOptions.TopicPath);
                        _logger.LogInformation($"Azure Service Bus created topic: {_asbOptions.TopicPath}");
                    }

                    if (!await mClient.SubscriptionExistsAsync(_asbOptions.TopicPath, _subscriptionName))
                    {
                        await mClient.CreateSubscriptionAsync(_asbOptions.TopicPath, _subscriptionName);
                        _logger.LogInformation($"Azure Service Bus topic {_asbOptions.TopicPath} created subscription: {_subscriptionName}");
                    }

                    _consumerClient = new SubscriptionClient(_asbOptions.ConnectionString, _asbOptions.TopicPath, _subscriptionName,
                        ReceiveMode.PeekLock, RetryPolicy.Default);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        #region private methods

        private Task OnConsumerReceived(Message message, CancellationToken token)
        {
            var header = message.UserProperties.ToDictionary(x => x.Key, y => y.Value?.ToString());
            header.Add(Headers.Group, _subscriptionName);

            var context = new TransportMessage(header, message.Body);

            OnMessageReceived?.Invoke(message.SystemProperties.LockToken, context);

            return Task.CompletedTask;
        }

        private Task OnExceptionReceived(ExceptionReceivedEventArgs args)
        {
            var context = args.ExceptionReceivedContext;
            var exceptionMessage =
                $"- Endpoint: {context.Endpoint}" + Environment.NewLine +
                $"- Entity Path: {context.EntityPath}" + Environment.NewLine +
                $"- Executing Action: {context.Action}" + Environment.NewLine +
                $"- Exception: {args.Exception}";

            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ExceptionReceived,
                Reason = exceptionMessage
            };

            OnLog?.Invoke(null, logArgs);

            return Task.CompletedTask;
        }

        private static void CheckValidSubscriptionName(string subscriptionName)
        {
            const string pathDelimiter = @"/";
            const int ruleNameMaximumLength = 50;
            char[] invalidEntityPathCharacters = { '@', '?', '#', '*' };

            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentNullException(subscriptionName);
            }

            // and "\" will be converted to "/" on the REST path anyway. Gateway/REST do not
            // have to worry about the begin/end slash problem, so this is purely a client side check.
            var tmpName = subscriptionName.Replace(@"\", pathDelimiter);
            if (tmpName.Length > ruleNameMaximumLength)
            {
                throw new ArgumentOutOfRangeException(subscriptionName, $@"Subscribe name '{subscriptionName}' exceeds the '{ruleNameMaximumLength}' character limit.");
            }

            if (tmpName.StartsWith(pathDelimiter, StringComparison.OrdinalIgnoreCase) ||
                tmpName.EndsWith(pathDelimiter, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($@"The subscribe name cannot contain '/' as prefix or suffix. The supplied value is '{subscriptionName}'", subscriptionName);
            }

            if (tmpName.Contains(pathDelimiter))
            {
                throw new ArgumentException($@"The subscribe name contains an invalid character '{pathDelimiter}'", subscriptionName);
            }

            foreach (var uriSchemeKey in invalidEntityPathCharacters)
            {
                if (subscriptionName.IndexOf(uriSchemeKey) >= 0)
                {
                    throw new ArgumentException($@"'{subscriptionName}' contains character '{uriSchemeKey}' which is not allowed because it is reserved in the Uri scheme.", subscriptionName);
                }
            }
        }

        #endregion private methods
    }
}
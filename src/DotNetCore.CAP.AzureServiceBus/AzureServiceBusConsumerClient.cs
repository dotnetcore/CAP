// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal sealed class AzureServiceBusConsumerClient : IConsumerClient
    {
        private readonly ILogger _logger;
        private readonly string _subscriptionName;
        private readonly AzureServiceBusOptions _asbOptions;

        private SubscriptionClient _consumerClient;

        private string _lockToken;

        public AzureServiceBusConsumerClient(
            ILogger logger,
            string subscriptionName,
            AzureServiceBusOptions options)
        {
            _logger = logger;
            _subscriptionName = subscriptionName;
            _asbOptions = options ?? throw new ArgumentNullException(nameof(options));

            InitAzureServiceBusClient().GetAwaiter().GetResult();
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

        public void Commit()
        {
            _consumerClient.CompleteAsync(_lockToken);
        }

        public void Reject()
        {
            // ignore
        }

        public void Dispose()
        {
            _consumerClient?.CloseAsync().Wait(1500);
        }

        #region private methods

        private async Task InitAzureServiceBusClient()
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

        private Task OnConsumerReceived(Message message, CancellationToken token)
        {
            _lockToken = message.SystemProperties.LockToken;
            var context = new MessageContext
            {
                Group = _subscriptionName,
                Name = message.Label,
                Content = Encoding.UTF8.GetString(message.Body)
            };

            OnMessageReceived?.Invoke(null, context);

            return Task.CompletedTask;
        }

        private Task OnExceptionReceived(ExceptionReceivedEventArgs args)
        {
            var context = args.ExceptionReceivedContext;
            var exceptionMessage =
                $"- Endpoint: {context.Endpoint}\r\n" +
                $"- Entity Path: {context.EntityPath}\r\n" +
                $"- Executing Action: {context.Action}\r\n" +
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
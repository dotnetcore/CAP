// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AzureServiceBus
{
    internal sealed class AzureServiceBusConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly ILogger _logger;
        private readonly string _subscriptionName;
        private readonly AzureServiceBusOptions _asbOptions;

        private ServiceBusAdministrationClient? _administrationClient;
        private ServiceBusClient? _serviceBusClient;
        private ServiceBusProcessor? _serviceBusProcessor;

        public AzureServiceBusConsumerClient(
            ILogger logger,
            string subscriptionName,
            IOptions<AzureServiceBusOptions> options)
        {
            
            _logger = logger;
            _subscriptionName = subscriptionName;
            _asbOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

        public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

        public BrokerAddress BrokerAddress => new ("AzureServiceBus", _asbOptions.ConnectionString);

        
        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            ConnectAsync().GetAwaiter().GetResult();

            topics = topics.Concat(_asbOptions!.SQLFilters?.Select(o => o.Key) ?? Enumerable.Empty<string>());
            var allRules = _administrationClient!.GetRulesAsync(_asbOptions!.TopicPath, _subscriptionName).ToEnumerable();
            var allRuleNames = allRules.Select(o => o.Name);

            
            foreach (var newRule in topics.Except(allRuleNames))
            {
                var isSqlRule = _asbOptions.SQLFilters?.FirstOrDefault(o => o.Key == newRule).Value is not null;

                RuleFilter? currentRuleToAdd = default;

                if (isSqlRule)
                {
                    var sqlExpression = _asbOptions.SQLFilters?.FirstOrDefault(o => o.Key == newRule).Value;
                    currentRuleToAdd = new SqlRuleFilter(sqlExpression);
                }
                else
                {
                    currentRuleToAdd = new CorrelationRuleFilter()
                    {
                        Subject = newRule
                    };
                }

                _administrationClient.CreateRuleAsync(_asbOptions.TopicPath, _subscriptionName,
                                        new CreateRuleOptions()
                                        {
                                            Name = newRule,
                                            Filter = currentRuleToAdd
                                        });

                _logger.LogInformation($"Azure Service Bus add rule: {newRule}");
            }

            foreach (var oldRule in allRuleNames.Except(topics))
            {
                _administrationClient.DeleteRuleAsync(_asbOptions.TopicPath, _subscriptionName, oldRule).GetAwaiter().GetResult();

                _logger.LogInformation($"Azure Service Bus remove rule: {oldRule}");
            }

        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConnectAsync().GetAwaiter().GetResult();

            _serviceBusProcessor!.ProcessMessageAsync += _serviceBusProcessor_ProcessMessageAsync;
            _serviceBusProcessor.ProcessErrorAsync += _serviceBusProcessor_ProcessErrorAsync;

            _serviceBusProcessor.StartProcessingAsync(cancellationToken).GetAwaiter().GetResult();
        }

        private Task _serviceBusProcessor_ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            
            var exceptionMessage =
                $"- Identifier: {args.Identifier}" + Environment.NewLine +
                $"- Entity Path: {args.EntityPath}" + Environment.NewLine +
                $"- Executing ErrorSource: {args.ErrorSource}" + Environment.NewLine +
                $"- Exception: {args.Exception}";

            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ExceptionReceived,
                Reason = exceptionMessage
            };

            OnLogCallback!(logArgs);

            return Task.CompletedTask;
        }

        private async Task _serviceBusProcessor_ProcessMessageAsync(ProcessMessageEventArgs arg)
        {
            var context = ConvertMessage(arg.Message);
            
            await OnMessageCallback!(context, new AzureServiceBusConsumerCommitInput(arg));
        }

        public void Commit(object? sender)
        {
            var commitInput = (AzureServiceBusConsumerCommitInput)sender!;
            if (_serviceBusProcessor?.AutoCompleteMessages ?? false)
            {
                commitInput.ProcessMessageArgs.CompleteMessageAsync(commitInput.ProcessMessageArgs.Message).GetAwaiter().GetResult();
            }
        }

        public void Reject(object? sender)
        {
            // ignore
        }

        public void Dispose()
        {
            if (!_serviceBusProcessor!.IsProcessing)
            {
                _serviceBusProcessor.DisposeAsync().GetAwaiter().GetResult();
            }
        }

        public async Task ConnectAsync()
        {
            if (_serviceBusProcessor != null)
            {
                return;
            }

            await _connectionLock.WaitAsync();

            try
            {
                if (_serviceBusProcessor == null)
                {
                    if (_asbOptions.TokenCredential != null)
                    {
                        _administrationClient = new ServiceBusAdministrationClient(_asbOptions.Namespace, _asbOptions.TokenCredential);
                        _serviceBusClient = new ServiceBusClient(_asbOptions.Namespace, _asbOptions.TokenCredential);
                    }
                    else
                    {
                        _administrationClient = new ServiceBusAdministrationClient(_asbOptions.ConnectionString);
                        _serviceBusClient = new ServiceBusClient(_asbOptions.ConnectionString);
                    }

                    var topicPaths = 
                        _asbOptions.CustomProducers.Select(producer => producer.TopicPath)
                            .Append(_asbOptions.TopicPath)
                            .Distinct();

                    foreach (var topicPath in topicPaths)
                    {
                        if (!await _administrationClient.TopicExistsAsync(topicPath))
                        {
                            await _administrationClient.CreateTopicAsync(topicPath);
                            _logger.LogInformation($"Azure Service Bus created topic: {topicPath}");
                        }

                        if (!await _administrationClient.SubscriptionExistsAsync(topicPath, _subscriptionName))
                        {
                            var subscriptionDescription =
                                new CreateSubscriptionOptions(topicPath, _subscriptionName)
                                {
                                    RequiresSession = _asbOptions.EnableSessions
                                };

                            await _administrationClient.CreateSubscriptionAsync(subscriptionDescription);
                            _logger.LogInformation($"Azure Service Bus topic {topicPath} created subscription: {_subscriptionName}");
                        }
                    }

                    _serviceBusProcessor = _serviceBusClient.CreateProcessor(_asbOptions.TopicPath, _subscriptionName,_asbOptions.EnableSessions?
                                           new ServiceBusProcessorOptions {
                                                AutoCompleteMessages =_asbOptions.AutoCompleteMessages,
                                                MaxConcurrentCalls=_asbOptions.MaxConcurrentCalls,
                                                MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(30),
                                           }:null);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        #region private methods

        private TransportMessage ConvertMessage(ServiceBusReceivedMessage message)
        {
            var headers = message.ApplicationProperties
                .ToDictionary(x => x.Key, y => y.Value?.ToString());
            
            headers.Add(Headers.Group, _subscriptionName);

            var customHeaders = _asbOptions.CustomHeaders?.Invoke(message);
            
            if (customHeaders?.Any() == true)
            {
                foreach (var customHeader in customHeaders)
                {
                    var added = headers.TryAdd(customHeader.Key, customHeader.Value);

                    if (!added)
                    {
                        _logger.LogWarning(
                            "Not possible to add the custom header {Header}. A value with the same key already exists in the Message headers.", 
                            customHeader.Key);
                    }
                }
            }
            
            return new TransportMessage(headers, message.Body);
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
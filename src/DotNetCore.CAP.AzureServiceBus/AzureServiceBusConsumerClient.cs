// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, SubscriptionClient> _consumerClientPool = new();

        public AzureServiceBusConsumerClient(
            ILogger logger,
            string subscriptionName,
            IOptions<AzureServiceBusOptions> options)
        {
            _logger = logger;
            _subscriptionName = subscriptionName;
            _asbOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public event EventHandler<TransportMessage>? OnMessageReceived;

        public event EventHandler<LogMessageEventArgs>? OnLog;

        public BrokerAddress BrokerAddress => new("AzureServiceBus", _asbOptions.ConnectionString);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            ConnectAsync().GetAwaiter().GetResult();

            Parallel.ForEach(_consumerClientPool, client =>
            {
                var allRuleNames = client.Value.GetRulesAsync()
                    .GetAwaiter()
                    .GetResult()
                    .Select(x => x.Name)
                    .ToList();

                foreach (var newRule in topics.Except(allRuleNames))
                {
                    CheckValidSubscriptionName(newRule);

                    client.Value.AddRuleAsync(new RuleDescription
                    {
                        Filter = new CorrelationFilter {Label = newRule},
                        Name = newRule
                    }).GetAwaiter().GetResult();

                    _logger.LogInformation($"Azure Service Bus add rule: {newRule}");
                }

                foreach (var oldRule in allRuleNames.Except(topics))
                {
                    client.Value.RemoveRuleAsync(oldRule).GetAwaiter().GetResult();

                    _logger.LogInformation($"Azure Service Bus remove rule: {oldRule}");
                }
            });
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConnectAsync().GetAwaiter().GetResult();
            foreach (var client in _consumerClientPool)
            {
                if (_asbOptions.EnableSessions)
                {
                    client.Value!.RegisterSessionHandler(OnConsumerReceivedWithSession,
                        new SessionHandlerOptions(OnExceptionReceived)
                        {
                            AutoComplete = false,
                            MaxAutoRenewDuration = TimeSpan.FromSeconds(30)
                        });
                }
                else
                {
                    client.Value!.RegisterMessageHandler(OnConsumerReceived,
                        new MessageHandlerOptions(OnExceptionReceived)
                        {
                            AutoComplete = false,
                            MaxConcurrentCalls = 10,
                            MaxAutoRenewDuration = TimeSpan.FromSeconds(30)
                        });
                }
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit(object sender)
        {
            var commitInput = (AzureServiceBusConsumerCommitInput) sender;
            if (_asbOptions.EnableSessions)
            {
                commitInput.Session?.CompleteAsync(commitInput.LockToken);
            }
            else
            {
                throw new NotImplementedException("How do we complete a message for the given client?");
                // _consumerClient!.CompleteAsync(commitInput.LockToken);
            }
        }

        public void Reject(object? sender)
        {
            // ignore
        }

        public void Dispose()
        {
            foreach (var client in _consumerClientPool)
            {
                client.Value.CloseAsync().GetAwaiter().GetResult();
            }
        }

        public async Task ConnectAsync()
        {
            // All clients are connected, need to think more about this
            if (_consumerClientPool.Count.Equals(_asbOptions.TopicPaths.Count()))
            {
                return;
            }

            await _connectionLock.WaitAsync();

            try
            {
                if (_consumerClientPool.Count == 0)
                {
                    var connectionStringBuilder = new ServiceBusConnectionStringBuilder(_asbOptions.ConnectionString);

                    var managementClient =
                        new ManagementClient(connectionStringBuilder, _asbOptions.ManagementTokenProvider);

                    foreach (var topicPath in _asbOptions.TopicPaths)
                    {
                        if (!await managementClient.TopicExistsAsync(topicPath))
                        {
                            await managementClient.CreateTopicAsync(topicPath);
                            _logger.LogInformation($"Azure Service Bus created topic: {topicPath}");
                        }

                        if (!await managementClient.SubscriptionExistsAsync(topicPath, _subscriptionName))
                        {
                            var subscriptionDescription =
                                new SubscriptionDescription(topicPath, _subscriptionName)
                                {
                                    RequiresSession = _asbOptions.EnableSessions
                                };

                            await managementClient.CreateSubscriptionAsync(subscriptionDescription);
                            _logger.LogInformation(
                                $"Azure Service Bus topic {topicPath} created subscription: {_subscriptionName}");
                        }

                        _consumerClientPool.TryAdd(topicPath, new SubscriptionClient(
                            connectionStringBuilder: connectionStringBuilder,
                            subscriptionName: _subscriptionName,
                            receiveMode: ReceiveMode.PeekLock,
                            retryPolicy: RetryPolicy.Default));
                    }
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        #region private methods

        private TransportMessage ConvertMessage(Message message)
        {
            var headers = message.UserProperties
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

        private Task OnConsumerReceivedWithSession(IMessageSession session, Message message, CancellationToken token)
        {
            var context = ConvertMessage(message);

            OnMessageReceived?.Invoke(
                new AzureServiceBusConsumerCommitInput(message.SystemProperties.LockToken, session), context);

            return Task.CompletedTask;
        }

        private Task OnConsumerReceived(Message message, CancellationToken token)
        {
            var context = ConvertMessage(message);

            OnMessageReceived?.Invoke(new AzureServiceBusConsumerCommitInput(message.SystemProperties.LockToken),
                context);

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
            char[] invalidEntityPathCharacters = {'@', '?', '#', '*'};

            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentNullException(subscriptionName);
            }

            // and "\" will be converted to "/" on the REST path anyway. Gateway/REST do not
            // have to worry about the begin/end slash problem, so this is purely a client side check.
            var tmpName = subscriptionName.Replace(@"\", pathDelimiter);
            if (tmpName.Length > ruleNameMaximumLength)
            {
                throw new ArgumentOutOfRangeException(subscriptionName,
                    $@"Subscribe name '{subscriptionName}' exceeds the '{ruleNameMaximumLength}' character limit.");
            }

            if (tmpName.StartsWith(pathDelimiter, StringComparison.OrdinalIgnoreCase) ||
                tmpName.EndsWith(pathDelimiter, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $@"The subscribe name cannot contain '/' as prefix or suffix. The supplied value is '{subscriptionName}'",
                    subscriptionName);
            }

            if (tmpName.Contains(pathDelimiter))
            {
                throw new ArgumentException($@"The subscribe name contains an invalid character '{pathDelimiter}'",
                    subscriptionName);
            }

            foreach (var uriSchemeKey in invalidEntityPathCharacters)
            {
                if (subscriptionName.IndexOf(uriSchemeKey) >= 0)
                {
                    throw new ArgumentException(
                        $@"'{subscriptionName}' contains character '{uriSchemeKey}' which is not allowed because it is reserved in the Uri scheme.",
                        subscriptionName);
                }
            }
        }

        #endregion private methods
    }
}
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
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private readonly ILogger _logger;
        private readonly AzureServiceBusOptions _asbOptions;

        public ConcurrentDictionary<AzureServiceBusSubscriptionConfigurator, SubscriptionClient?> ConsumerClientPool { get; private set; }  = new();

        private AzureServiceBusSubscriptionConfigurator? DefaultSubscriptionConfigurator =>
            ConsumerClientPool
                .SingleOrDefault(c => c.Key.Default == true)
                .Key;

        public AzureServiceBusConsumerClient(
            ILogger logger,
            string defaultSubscriptionName,
            IOptions<AzureServiceBusOptions> options)
        {
            _logger = logger;
            _asbOptions = options.Value ?? throw new ArgumentNullException(nameof(options));

            SetupSubscriptionsPool(defaultSubscriptionName, _asbOptions);
        }

        private void SetupSubscriptionsPool(string defaultSubscriptionName, AzureServiceBusOptions options)
        {
            var defaultSubscriber = new AzureServiceBusSubscriptionConfigurator(
                options.TopicPath, defaultSubscriptionName);

            var customSubscribers = options.CustomSubscribersConfiguration
                .Select(c => new AzureServiceBusSubscriptionConfigurator(c))
                .ToList();

            customSubscribers.Add(defaultSubscriber);

            foreach (var customSubscriber in customSubscribers)
            {
                var added = ConsumerClientPool.TryAdd(customSubscriber, null);

                if (!added)
                    _logger.LogWarning("Subscription {SubscriptionName} for Topic {TopicName} was not created.",
                        customSubscriber.SubscriptionName, customSubscriber.TopicPath);
            }
        }

        public event EventHandler<TransportMessage>? OnMessageReceived;

        public event EventHandler<LogMessageEventArgs>? OnLog;

        public BrokerAddress BrokerAddress => new("AzureServiceBus", _asbOptions.ConnectionString);


        public async Task ConnectAsync()
        {
            if (ConsumerClientPool.All(c => c.Value != null))
            {
                return;
            }

            await _connectionLock.WaitAsync();

            try
            {
                foreach (var consumerClient in
                         ConsumerClientPool
                             .Where(c => c.Value == null))
                {
                    var connectionStringBuilder = new ServiceBusConnectionStringBuilder(_asbOptions.ConnectionString);

                    var managementClient =
                        new ManagementClient(connectionStringBuilder, _asbOptions.ManagementTokenProvider);

                    var client =
                        await CreateSubscriptionClient(managementClient, consumerClient.Key, connectionStringBuilder);

                    ConsumerClientPool.TryUpdate(consumerClient.Key, client, null);
                }
            }
            finally

            {
                _connectionLock.Release();
            }
        }

        private async Task<SubscriptionClient> CreateSubscriptionClient(ManagementClient managementClient,
            AzureServiceBusSubscriptionConfigurator configurator,
            ServiceBusConnectionStringBuilder connectionStringBuilder)
        {
            await CreateTopicIfNotExistsAsync(managementClient, configurator);

            await CreateSubscriptionIfNotExistsAsync(managementClient, configurator);

            return new SubscriptionClient(
                connectionStringBuilder: connectionStringBuilder,
                subscriptionName: configurator.SubscriptionName,
                receiveMode: configurator.ReceiveMode ?? ReceiveMode.PeekLock,
                retryPolicy: configurator.RetryPolicy ?? RetryPolicy.Default);
        }

        private async Task CreateSubscriptionIfNotExistsAsync(ManagementClient managementClient,
            AzureServiceBusSubscriptionConfigurator customServiceBusSubscriberOptions)
        {
            if (!await managementClient.SubscriptionExistsAsync(customServiceBusSubscriberOptions.TopicPath,
                    customServiceBusSubscriberOptions.SubscriptionName))
            {
                var subscriptionDescription =
                    new SubscriptionDescription(
                        customServiceBusSubscriberOptions.TopicPath,
                        customServiceBusSubscriberOptions.SubscriptionName)
                    {
                        RequiresSession = _asbOptions.EnableSessions
                    };

                await managementClient.CreateSubscriptionAsync(subscriptionDescription);

                _logger.LogInformation(
                    $"Azure Service Bus topic {customServiceBusSubscriberOptions.TopicPath} created subscription: {customServiceBusSubscriberOptions.SubscriptionName}");
            }
        }

        private async Task CreateTopicIfNotExistsAsync(ManagementClient managementClient,
            AzureServiceBusSubscriptionConfigurator customServiceBusSubscriberOptions)
        {
            if (!await managementClient.TopicExistsAsync(customServiceBusSubscriberOptions.TopicPath))
            {
                await managementClient.CreateTopicAsync(customServiceBusSubscriberOptions.TopicPath);
                _logger.LogInformation(
                    $"Azure Service Bus created topic: {customServiceBusSubscriberOptions.TopicPath}");
            }
        }

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            ConnectAsync().GetAwaiter().GetResult();

            Parallel.ForEach(ConsumerClientPool, entry =>
            {
                var existingSubscriptionRules = entry.Value.GetRulesAsync()
                    .GetAwaiter()
                    .GetResult()
                    .Select(x => x.Name)
                    .ToList();

                foreach (var newRule in topics.Except(existingSubscriptionRules))
                {
                    CheckValidSubscriptionName(newRule);

                    entry.Value.AddRuleAsync(new RuleDescription
                    {
                        Filter = new CorrelationFilter {Label = newRule},
                        Name = newRule
                    }).GetAwaiter().GetResult();

                    _logger.LogInformation($"Azure Service Bus add rule: {newRule}");
                }

                foreach (var oldRule in existingSubscriptionRules.Except(topics))
                {
                    entry.Value.RemoveRuleAsync(oldRule).GetAwaiter().GetResult();

                    _logger.LogInformation($"Azure Service Bus remove rule: {oldRule}");
                }
            });
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConnectAsync().GetAwaiter().GetResult();
            foreach (var entry in ConsumerClientPool)
            {
                if (entry.Key.EnableSessions)
                {
                    entry.Value!.RegisterSessionHandler(OnConsumerReceivedWithSession,
                        new SessionHandlerOptions(OnExceptionReceived)
                        {
                            AutoComplete = false,
                            MaxAutoRenewDuration = TimeSpan.FromSeconds(30)
                        });
                }
                else
                {
                    entry.Value!.RegisterMessageHandler(OnConsumerReceived,
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

        private Task OnConsumerReceivedWithSession(IMessageSession session, Message message, CancellationToken token)
        {
            var context = ConvertMessage(message);

            var destination = GetIncomingTopicPath(context);

            var commitInput =
                new AzureServiceBusConsumerCommitInput(message.SystemProperties.LockToken, session, destination);

            OnMessageReceived?.Invoke(commitInput, context);

            return Task.CompletedTask;
        }

        private Task OnConsumerReceived(Message message, CancellationToken token)
        {
            var context = ConvertMessage(message);

            var destination = GetIncomingTopicPath(context);

            var commitInput = new AzureServiceBusConsumerCommitInput(message.SystemProperties.LockToken, destination);

            OnMessageReceived?.Invoke(commitInput, context);

            return Task.CompletedTask;
        }

        private string? GetIncomingTopicPath(TransportMessage context)
        {
            var destination =
                context.Headers.TryGetValue(CAP.Messages.Headers.Destination, out var destinationHeader)
                    ? destinationHeader
                    : DefaultSubscriptionConfigurator?.TopicPath;
            return destination;
        }

        private TransportMessage ConvertMessage(Message message)
        {
            var headers = message.UserProperties
                .ToDictionary(x => x.Key, y => y.Value?.ToString());

            headers.Add(Headers.Group, DefaultSubscriptionConfigurator?.SubscriptionName);

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

        public void Commit(object sender)
        {
            var commitInput = (AzureServiceBusConsumerCommitInput) sender;

            var consumerClient = ConsumerClientPool
                .SingleOrDefault(c => c.Key.TopicPath == commitInput.DestinationTopicPath)
                .Value;

            if (_asbOptions.EnableSessions)
            {
                commitInput.Session?.CompleteAsync(commitInput.LockToken);
            }
            else
            {
                consumerClient.CompleteAsync(commitInput.LockToken);
            }
        }

        public void Reject(object? sender)
        {
            // ignore
        }

        public void Dispose()
        {
            foreach (var client in ConsumerClientPool)
            {
                client.Value.CloseAsync().GetAwaiter().GetResult();
            }
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
    }
}
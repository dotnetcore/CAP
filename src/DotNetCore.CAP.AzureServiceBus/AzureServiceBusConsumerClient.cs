// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using DotNetCore.CAP.AzureServiceBus.Helpers;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AzureServiceBus;

internal sealed class AzureServiceBusConsumerClient : IConsumerClient
{
    private readonly AzureServiceBusOptions _asbOptions;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _subscriptionName;
    private readonly byte _groupConcurrent;
    private readonly SemaphoreSlim _semaphore;

    private ServiceBusAdministrationClient? _administrationClient;
    private ServiceBusClient? _serviceBusClient;
    private ServiceBusProcessorFacade? _serviceBusProcessor;

    public AzureServiceBusConsumerClient(
        ILogger logger,
        string subscriptionName,
        byte groupConcurrent,
        IOptions<AzureServiceBusOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _subscriptionName = subscriptionName;
        _groupConcurrent = groupConcurrent;
        _semaphore = new SemaphoreSlim(groupConcurrent);
        _serviceProvider = serviceProvider;
        _asbOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

    public BrokerAddress BrokerAddress => ServiceBusHelpers.GetBrokerAddress(_asbOptions.ConnectionString, _asbOptions.Namespace);

    public void Subscribe(IEnumerable<string> topics)
    {
        if (topics == null) throw new ArgumentNullException(nameof(topics));

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
                var correlationRule = new CorrelationRuleFilter
                {
                    Subject = newRule
                };

                foreach (var correlationHeader in _asbOptions.DefaultCorrelationHeaders)
                {
                    correlationRule.ApplicationProperties.Add(correlationHeader.Key, correlationHeader.Value);
                }

                currentRuleToAdd = correlationRule;
            }

            _administrationClient.CreateRuleAsync(_asbOptions.TopicPath, _subscriptionName,
                new CreateRuleOptions
                {
                    Name = newRule,
                    Filter = currentRuleToAdd
                });

            _logger.LogInformation($"Azure Service Bus add rule: {newRule}");
        }

        foreach (var oldRule in allRuleNames.Except(topics))
        {
            _administrationClient.DeleteRuleAsync(_asbOptions.TopicPath, _subscriptionName, oldRule).GetAwaiter()
                .GetResult();

            _logger.LogInformation($"Azure Service Bus remove rule: {oldRule}");
        }
    }

    public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
    {
        ConnectAsync().GetAwaiter().GetResult();

        if (_serviceBusProcessor!.IsSessionProcessor)
        {
            _serviceBusProcessor!.ProcessSessionMessageAsync += _serviceBusProcessor_ProcessSessionMessageAsync;
        }
        else
        {
            _serviceBusProcessor!.ProcessMessageAsync += _serviceBusProcessor_ProcessMessageAsync;
        }

        _serviceBusProcessor.ProcessErrorAsync += _serviceBusProcessor_ProcessErrorAsync;

        _serviceBusProcessor.StartProcessingAsync(cancellationToken).GetAwaiter().GetResult();
    }

    public void Commit(object? sender)
    {
        var commitInput = (AzureServiceBusConsumerCommitInput)sender!;
        if (_serviceBusProcessor?.AutoCompleteMessages ?? false)
            commitInput.CompleteMessageAsync().GetAwaiter().GetResult();
    }

    public void Reject(object? sender)
    {
        var commitInput = (AzureServiceBusConsumerCommitInput)sender!;
        commitInput.AbandonMessageAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (!_serviceBusProcessor!.IsProcessing) _serviceBusProcessor.DisposeAsync().GetAwaiter().GetResult();
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

        if (_groupConcurrent > 0)
        {
            await _semaphore.WaitAsync();
            _ = Task.Run(() => OnMessageCallback!(context, new AzureServiceBusConsumerCommitInput(arg))).ConfigureAwait(false);
        }
        else
        {
            await OnMessageCallback!(context, new AzureServiceBusConsumerCommitInput(arg));
        }
    }

    private async Task _serviceBusProcessor_ProcessSessionMessageAsync(ProcessSessionMessageEventArgs arg)
    {
        var context = ConvertMessage(arg.Message);

        await OnMessageCallback!(context, new AzureServiceBusConsumerCommitInput(arg));
    }

    public async Task ConnectAsync()
    {
        if (_serviceBusProcessor != null) return;

        await _connectionLock.WaitAsync();

        try
        {
            if (_serviceBusProcessor == null)
            {
                if (_asbOptions.TokenCredential != null)
                {
                    _administrationClient =
                        new ServiceBusAdministrationClient(_asbOptions.Namespace, _asbOptions.TokenCredential);
                    _serviceBusClient = new ServiceBusClient(_asbOptions.Namespace, _asbOptions.TokenCredential);
                }
                else
                {
                    _administrationClient = new ServiceBusAdministrationClient(_asbOptions.ConnectionString);
                    _serviceBusClient = new ServiceBusClient(_asbOptions.ConnectionString);
                }

                var topicConfigs =
                    _asbOptions.CustomProducers.Select(producer =>
                            (topicPaths: producer.TopicPath, subscribe: producer.CreateSubscription))
                        .Append((topicPaths: _asbOptions.TopicPath, subscribe: true))
                        .GroupBy(n => n.topicPaths, StringComparer.OrdinalIgnoreCase)
                        .Select(n => (topicPaths: n.Key, subscribe: n.Max(o => o.subscribe)));

                foreach (var (topicPath, subscribe) in topicConfigs)
                {
                    if (!await _administrationClient.TopicExistsAsync(topicPath))
                    {
                        await _administrationClient.CreateTopicAsync(topicPath);
                        _logger.LogInformation($"Azure Service Bus created topic: {topicPath}");
                    }

                    if (subscribe && !await _administrationClient.SubscriptionExistsAsync(topicPath, _subscriptionName))
                    {
                        var subscriptionDescription =
                            new CreateSubscriptionOptions(topicPath, _subscriptionName)
                            {
                                RequiresSession = _asbOptions.EnableSessions,
                                AutoDeleteOnIdle = _asbOptions.SubscriptionAutoDeleteOnIdle,
                                LockDuration = _asbOptions.SubscriptionMessageLockDuration,
                                DefaultMessageTimeToLive = _asbOptions.SubscriptionDefaultMessageTimeToLive,
                                MaxDeliveryCount = _asbOptions.SubscriptionMaxDeliveryCount,
                            };

                        await _administrationClient.CreateSubscriptionAsync(subscriptionDescription);

                        _logger.LogInformation(
                            $"Azure Service Bus topic {topicPath} created subscription: {_subscriptionName}");
                    }
                }

                _serviceBusProcessor = !_asbOptions.EnableSessions
                    ? new ServiceBusProcessorFacade(
                        serviceBusProcessor: _serviceBusClient.CreateProcessor(_asbOptions.TopicPath,
                            _subscriptionName,
                            new ServiceBusProcessorOptions
                            {
                                AutoCompleteMessages = _asbOptions.AutoCompleteMessages,
                                MaxConcurrentCalls = _asbOptions.MaxConcurrentCalls,
                                MaxAutoLockRenewalDuration = _asbOptions.MaxAutoLockRenewalDuration,
                            }))
                    : new ServiceBusProcessorFacade(
                        serviceBusSessionProcessor: _serviceBusClient.CreateSessionProcessor(_asbOptions.TopicPath,
                            _subscriptionName,
                            new ServiceBusSessionProcessorOptions
                            {
                                AutoCompleteMessages = _asbOptions.AutoCompleteMessages,
                                MaxConcurrentCallsPerSession = _asbOptions.MaxConcurrentCalls,
                                MaxAutoLockRenewalDuration = _asbOptions.MaxAutoLockRenewalDuration,
                                MaxConcurrentSessions = _asbOptions.MaxConcurrentSessions,
                                SessionIdleTimeout = _asbOptions.SessionIdleTimeout,
                            }));
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

        if (_asbOptions.CustomHeadersBuilder != null)
        {
            var customHeaders = _asbOptions.CustomHeadersBuilder(message, _serviceProvider);
            foreach (var customHeader in customHeaders)
            {
                var added = headers.TryAdd(customHeader.Key, customHeader.Value);

                if (!added)
                    _logger.LogWarning("Not possible to add the custom header {Header}. A value with the same key already exists in the Message headers.",customHeader.Key);
            }
        }

        return new TransportMessage(headers, message.Body);
    }

    private static void CheckValidSubscriptionName(string subscriptionName)
    {
        const string pathDelimiter = @"/";
        const int ruleNameMaximumLength = 50;
        char[] invalidEntityPathCharacters = { '@', '?', '#', '*' };

        if (string.IsNullOrWhiteSpace(subscriptionName)) throw new ArgumentNullException(subscriptionName);

        // and "\" will be converted to "/" on the REST path anyway. Gateway/REST do not
        // have to worry about the begin/end slash problem, so this is purely a client side check.
        var tmpName = subscriptionName.Replace(@"\", pathDelimiter);
        if (tmpName.Length > ruleNameMaximumLength)
            throw new ArgumentOutOfRangeException(subscriptionName,
                $@"Subscribe name '{subscriptionName}' exceeds the '{ruleNameMaximumLength}' character limit.");

        if (tmpName.StartsWith(pathDelimiter, StringComparison.OrdinalIgnoreCase) ||
            tmpName.EndsWith(pathDelimiter, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $@"The subscribe name cannot contain '/' as prefix or suffix. The supplied value is '{subscriptionName}'",
                subscriptionName);

        if (tmpName.Contains(pathDelimiter))
            throw new ArgumentException($@"The subscribe name contains an invalid character '{pathDelimiter}'",
                subscriptionName);

        foreach (var uriSchemeKey in invalidEntityPathCharacters)
        {
            if (subscriptionName.IndexOf(uriSchemeKey) >= 0)
                throw new ArgumentException(
                    $@"'{subscriptionName}' contains character '{uriSchemeKey}' which is not allowed because it is reserved in the Uri scheme.",
                    subscriptionName);
        }
    }

    #endregion private methods
}
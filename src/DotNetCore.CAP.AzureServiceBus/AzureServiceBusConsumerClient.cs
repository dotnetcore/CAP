// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
        private readonly ILogger _logger;
        private readonly string _subscriptionName;
        private readonly IServiceProvider _serviceProvider;
        private readonly AzureServiceBusOptions _asbOptions;

        private ServiceBusAdministrationClient? _administrationClient;
        private ServiceBusClient? _serviceBusClient;
        private readonly ConcurrentBag<ServiceBusProcessor> _serviceBusProcessors = new();

        public AzureServiceBusConsumerClient(
            ILogger logger,
            string subscriptionName,
            IOptions<AzureServiceBusOptions> options,
            IServiceProvider serviceProvider)
        {
            
            _logger = logger;
            _subscriptionName = subscriptionName;
            _serviceProvider = serviceProvider;
            _asbOptions = options.Value ?? throw new ArgumentNullException(nameof(options));

            Connect();
        }

        public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

        public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

        public BrokerAddress BrokerAddress => new ("AzureServiceBus", _asbOptions.ConnectionString);

        public ICollection<string> FetchTopics(IEnumerable<string> receiveTopics)
        {
            var topicPaths = receiveTopics
                .Distinct()
                .ToArray();

            CreateTopicsAndSubscriptionIfNotExistAsync(topicPaths).GetAwaiter().GetResult();

            return topicPaths;
        }

        public void Subscribe(IEnumerable<string> receiveTopics)
        {
            foreach (var topic in receiveTopics)
            {
                _serviceBusProcessors.Add(_serviceBusClient.CreateProcessor(topic, _subscriptionName,
                    new ServiceBusProcessorOptions
                    {
                        MaxConcurrentCalls = _asbOptions.MaxConcurrentCalls,
                        MaxAutoLockRenewalDuration = _asbOptions.LockRenewalDuration
                    }));
            }
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            foreach (var processor in _serviceBusProcessors)
            {
                processor.ProcessMessageAsync += _serviceBusProcessor_ProcessMessageAsync;
                processor.ProcessErrorAsync += _serviceBusProcessor_ProcessErrorAsync;

                processor.StartProcessingAsync(cancellationToken).GetAwaiter().GetResult();
            }

            //continue the infinite loop to keep the object alive...
            while (true)
            {
                Task.Delay(1000, cancellationToken);
            }
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

            commitInput.ProcessMessageArgs.CompleteMessageAsync(commitInput.ProcessMessageArgs.Message).GetAwaiter().GetResult();
        }

        public void Reject(object? sender)
        {
            // ignore
        }

        public void Dispose()
        {
            foreach (var processor in _serviceBusProcessors)
            {
                processor.DisposeAsync().GetAwaiter().GetResult();
            }

            _serviceBusClient?.DisposeAsync().GetAwaiter().GetResult();
        }

        public void Connect()
        {
            if (_asbOptions.TokenCredential != null)
            {
                _administrationClient =
                    new ServiceBusAdministrationClient(_asbOptions.Namespace, _asbOptions.TokenCredential);
                _serviceBusClient =
                    new ServiceBusClient(_asbOptions.Namespace, _asbOptions.TokenCredential);
            }
            else
            {
                _administrationClient = new ServiceBusAdministrationClient(_asbOptions.ConnectionString);
                _serviceBusClient = new ServiceBusClient(_asbOptions.ConnectionString);
            }
        }

        #region private methods

        private async Task CreateTopicsAndSubscriptionIfNotExistAsync(params string[] topicPaths)
        {
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
                            AutoDeleteOnIdle = _asbOptions.SubscriptionAutoDeleteOnIdle
                        };

                    await _administrationClient.CreateSubscriptionAsync(subscriptionDescription);
                    _logger.LogInformation($"Azure Service Bus topic {topicPath} created subscription: {_subscriptionName}");
                }
            }
        }

        private TransportMessage ConvertMessage(ServiceBusReceivedMessage message)
        {
            var headers = message.ApplicationProperties
                .ToDictionary(x => x.Key, y => y.Value?.ToString());
            
            headers.Add(Headers.Group, _subscriptionName);

            List<KeyValuePair<string, string>>? customHeaders = null;

            if (_asbOptions.CustomHeadersBuilder != null)
            {
                customHeaders = _asbOptions.CustomHeadersBuilder(message,_serviceProvider).ToList();
            }
            
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
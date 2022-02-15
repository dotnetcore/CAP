// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    public class KafkaConsumerClient : IConsumerClient
    {
        private static readonly SemaphoreSlim ConnectionLock = new(initialCount: 1, maxCount: 1);

        private readonly string _groupId;
        private readonly KafkaOptions _kafkaOptions;
        private IConsumer<string, byte[]>? _consumerClient;

        public KafkaConsumerClient(string groupId, IOptions<KafkaOptions> options)
        {
            _groupId = groupId;
            _kafkaOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public event EventHandler<TransportMessage>? OnMessageReceived;

        public event EventHandler<LogMessageEventArgs>? OnLog;

        public BrokerAddress BrokerAddress => new("Kafka", _kafkaOptions.Servers);

        public ICollection<string> FetchTopics(IEnumerable<string> topicNames)
        {
            if (topicNames == null)
            {
                throw new ArgumentNullException(nameof(topicNames));
            }

            var regexTopicNames = topicNames.Select(Helper.WildcardToRegex).ToList();

            try
            {
                var config = new AdminClientConfig(_kafkaOptions.MainConfig) { BootstrapServers = _kafkaOptions.Servers };

                using var adminClient = new AdminClientBuilder(config).Build();

                adminClient.CreateTopicsAsync(regexTopicNames.Select(x => new TopicSpecification
                {
                    Name = x
                })).GetAwaiter().GetResult();
            }
            catch (CreateTopicsException ex) when (ex.Message.Contains("already exists"))
            {
            }
            catch (Exception ex)
            {
                var logArgs = new LogMessageEventArgs
                {
                    LogType = MqLogType.ConsumeError,
                    Reason = $"An error was encountered when automatically creating topic! -->" + ex.Message
                };
                OnLog?.Invoke(null, logArgs);
            }

            return regexTopicNames;
        }

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();

            _consumerClient!.Subscribe(topics);
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            while (true)
            {
                ConsumeResult<string, byte[]> consumerResult;

                try
                {
                    consumerResult = _consumerClient!.Consume(cancellationToken);
                }
                catch (ConsumeException e) when (_kafkaOptions.RetriableErrorCodes.Contains(e.Error.Code))
                {
                    var logArgs = new LogMessageEventArgs
                    {
                        LogType = MqLogType.ConsumeRetries,
                        Reason = e.Error.ToString()
                    };
                    OnLog?.Invoke(null, logArgs);

                    continue;
                }

                if (consumerResult.IsPartitionEOF || consumerResult.Message.Value == null) continue;

                var headers = new Dictionary<string, string?>(consumerResult.Message.Headers.Count);
                foreach (var header in consumerResult.Message.Headers)
                {
                    var val = header.GetValueBytes();
                    headers.Add(header.Key, val != null ? Encoding.UTF8.GetString(val) : null);
                }
                headers.Add(Messages.Headers.Group, _groupId);

                if (_kafkaOptions.CustomHeaders != null)
                {
                    var customHeaders = _kafkaOptions.CustomHeaders(consumerResult);
                    foreach (var customHeader in customHeaders)
                    {
                        headers[customHeader.Key] = customHeader.Value;
                    }
                }

                var message = new TransportMessage(headers, consumerResult.Message.Value);

                OnMessageReceived?.Invoke(consumerResult, message);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit(object sender)
        {
            _consumerClient!.Commit((ConsumeResult<string, byte[]>)sender);
        }

        public void Reject(object? sender)
        {
            _consumerClient!.Assign(_consumerClient.Assignment);
        }

        public void Dispose()
        {
            _consumerClient?.Dispose();
        }

        public void Connect()
        {
            if (_consumerClient != null)
            {
                return;
            }

            ConnectionLock.Wait();

            try
            {
                if (_consumerClient == null)
                {
                    var config = new ConsumerConfig(new Dictionary<string, string>(_kafkaOptions.MainConfig));
                    config.BootstrapServers ??= _kafkaOptions.Servers;
                    config.GroupId ??= _groupId;
                    config.AutoOffsetReset ??= AutoOffsetReset.Earliest;
                    config.AllowAutoCreateTopics ??= true;
                    config.EnableAutoCommit ??= false;
                    config.LogConnectionClose ??= false;

                    _consumerClient = BuildConsumer(config);
                }
            }
            finally
            {
                ConnectionLock.Release();
            }
        }

        protected virtual IConsumer<string, byte[]> BuildConsumer(ConsumerConfig config)
        {
            return new ConsumerBuilder<string, byte[]>(config)
                .SetErrorHandler(ConsumerClient_OnConsumeError)
                .Build();
        }

        private void ConsumerClient_OnConsumeError(IConsumer<string, byte[]> consumer, Error e)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ServerConnError,
                Reason = $"An error occurred during connect kafka --> {e.Reason}"
            };
            OnLog?.Invoke(null, logArgs);
        }
    }
}
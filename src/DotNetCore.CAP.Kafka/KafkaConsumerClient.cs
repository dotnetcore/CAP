// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Confluent.Kafka;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    internal sealed class KafkaConsumerClient : IConsumerClient
    {
        private static readonly SemaphoreSlim ConnectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly string _groupId;
        private readonly KafkaOptions _kafkaOptions;
        private IConsumer<string, byte[]> _consumerClient;

        public KafkaConsumerClient(string groupId, IOptions<KafkaOptions> options)
        {
            _groupId = groupId;
            _kafkaOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("Kafka", _kafkaOptions.Servers);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();

            _consumerClient.Subscribe(topics);
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            while (true)
            {
                var consumerResult = _consumerClient.Consume(cancellationToken);

                if (consumerResult.IsPartitionEOF || consumerResult.Message.Value == null) continue;

                var headers = new Dictionary<string, string>(consumerResult.Message.Headers.Count);
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
                        headers.Add(customHeader.Key, customHeader.Value);
                    }
                }

                var message = new TransportMessage(headers, consumerResult.Message.Value);

                OnMessageReceived?.Invoke(consumerResult, message);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit(object sender)
        {
            _consumerClient.Commit((ConsumeResult<string, byte[]>)sender);
        }

        public void Reject(object sender)
        {
            _consumerClient.Assign(_consumerClient.Assignment);
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
                    _kafkaOptions.MainConfig["group.id"] = _groupId;
                    _kafkaOptions.MainConfig["auto.offset.reset"] = "earliest";
                    var config = _kafkaOptions.AsKafkaConfig();

                    _consumerClient = new ConsumerBuilder<string, byte[]>(config)
                        .SetErrorHandler(ConsumerClient_OnConsumeError)
                        .Build();
                }
            }
            finally
            {
                ConnectionLock.Release();
            }
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
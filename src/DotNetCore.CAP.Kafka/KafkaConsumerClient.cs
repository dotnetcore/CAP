// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    internal sealed class KafkaConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly string _groupId;
        private readonly KafkaOptions _kafkaOptions;
        private IConsumer<Null, string> _consumerClient;

        public KafkaConsumerClient(string groupId, IOptions<KafkaOptions> options)
        {
            _groupId = groupId;
            _kafkaOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public event EventHandler<MessageContext> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public string ServersAddress => _kafkaOptions.Servers;

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

                if (consumerResult.IsPartitionEOF || consumerResult.Value == null) continue;

                var message = new MessageContext
                {
                    Group = _groupId,
                    Name = consumerResult.Topic,
                    Content = consumerResult.Value
                };

                OnMessageReceived?.Invoke(consumerResult, message);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit()
        {
            _consumerClient.Commit();
        }

        public void Reject()
        {
            _consumerClient.Assign(_consumerClient.Assignment);
        }

        public void Dispose()
        {
            _consumerClient?.Dispose();
        }

        #region private methods

        private void Connect()
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
                    _kafkaOptions.MainConfig["group.id"] = _groupId;
                    _kafkaOptions.MainConfig["auto.offset.reset"] = "earliest";
                    var config = _kafkaOptions.AsKafkaConfig();

                    _consumerClient = new ConsumerBuilder<Null, string>(config)
                        .SetErrorHandler(ConsumerClient_OnConsumeError)
                        .Build();
                }
            }
            finally
            {
                _connectionLock.Release();
            } 
        }

        private void ConsumerClient_OnConsumeError(IConsumer<Null, string> consumer, Error e)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ServerConnError,
                Reason = $"An error occurred during connect kafka --> {e.Reason}"
            };
            OnLog?.Invoke(null, logArgs);
        }

        #endregion private methods
    }
}
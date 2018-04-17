// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;

namespace DotNetCore.CAP.Kafka
{
    internal sealed class KafkaConsumerClient : IConsumerClient
    {
        private readonly string _groupId;
        private readonly KafkaOptions _kafkaOptions;
        private Consumer<Null, string> _consumerClient;

        public KafkaConsumerClient(string groupId, KafkaOptions options)
        {
            _groupId = groupId;
            _kafkaOptions = options ?? throw new ArgumentNullException(nameof(options));
            StringDeserializer = new StringDeserializer(Encoding.UTF8);
        }

        public IDeserializer<string> StringDeserializer { get; set; }

        public event EventHandler<MessageContext> OnMessageReceived;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public string ServersAddress => _kafkaOptions.Servers;

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            if (_consumerClient == null)
            {
                InitKafkaClient();
            }

            _consumerClient.Subscribe(topics);
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _consumerClient.Poll(timeout);
            }

            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit()
        {
            _consumerClient.CommitAsync();
        }

        public void Reject()
        {
            _consumerClient.Assign(_consumerClient.Assignment);
        }

        public void Dispose()
        {
            _consumerClient.Dispose();
        }

        #region private methods

        private void InitKafkaClient()
        {
            lock (_kafkaOptions)
            {
                _kafkaOptions.MainConfig["group.id"] = _groupId;

                var config = _kafkaOptions.AsKafkaConfig();
                _consumerClient = new Consumer<Null, string>(config, null, StringDeserializer);
                _consumerClient.OnConsumeError += ConsumerClient_OnConsumeError;
                _consumerClient.OnMessage += ConsumerClient_OnMessage;
                _consumerClient.OnError += ConsumerClient_OnError;
            }
        }

        private void ConsumerClient_OnConsumeError(object sender, Message e)
        {
            var message = e.Deserialize<Null, string>(null, StringDeserializer);
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ConsumeError,
                Reason = $"An error occurred during consume the message; Topic:'{e.Topic}'," +
                         $"Message:'{message.Value}', Reason:'{e.Error}'."
            };
            OnLog?.Invoke(sender, logArgs);
        }

        private void ConsumerClient_OnMessage(object sender, Message<Null, string> e)
        {
            var message = new MessageContext
            {
                Group = _groupId,
                Name = e.Topic,
                Content = e.Value
            };

            OnMessageReceived?.Invoke(sender, message);
        }

        private void ConsumerClient_OnError(object sender, Error e)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ServerConnError,
                Reason = e.ToString()
            };
            OnLog?.Invoke(sender, logArgs);
        }

        #endregion private methods
    }
}
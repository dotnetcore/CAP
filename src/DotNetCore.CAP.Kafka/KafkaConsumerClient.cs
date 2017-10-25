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

        public event EventHandler<string> OnError;

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
                throw new ArgumentNullException(nameof(topics));

            if (_consumerClient == null)
                InitKafkaClient();

            //_consumerClient.Assign(topics.Select(x=> new TopicPartition(x, 0)));
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
            // Ignore, Kafka will not commit offset when not commit.
        }

        public void Dispose()
        {
            _consumerClient.Dispose();
        }

        #region private methods

        private void InitKafkaClient()
        {
            _kafkaOptions.MainConfig["group.id"] = _groupId;

            var config = _kafkaOptions.AsKafkaConfig();
            _consumerClient = new Consumer<Null, string>(config, null, StringDeserializer);
            _consumerClient.OnConsumeError += ConsumerClient_OnConsumeError;
            _consumerClient.OnMessage += ConsumerClient_OnMessage;
            _consumerClient.OnError += ConsumerClient_OnError;
        }

        private void ConsumerClient_OnConsumeError(object sender, Message e)
        {
            var message = e.Deserialize<Null, string>(null, StringDeserializer);

            OnError?.Invoke(sender, $"An error occurred during consume the message; Topic:'{e.Topic}'," +
                $"Message:'{message.Value}', Reason:'{e.Error}'.");
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
            OnError?.Invoke(sender, e.ToString());
        }

        #endregion private methods
    }
}
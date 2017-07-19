using System;
using System.Text;
using System.Threading;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;

namespace DotNetCore.CAP.Kafka
{
    public class KafkaConsumerClient : IConsumerClient
    {
        private readonly string _groupId;
        private readonly KafkaOptions _kafkaOptions;
        private Consumer<Null, string> _consumerClient;

        public event EventHandler<MessageContext> MessageReceieved;

        public IDeserializer<string> StringDeserializer { get; set; }

        public KafkaConsumerClient(string groupId, KafkaOptions options)
        {
            _groupId = groupId;
            _kafkaOptions = options;
            StringDeserializer = new StringDeserializer(Encoding.UTF8);
        }

        public void Subscribe(string topic)
        {
            Subscribe(topic, 0);
        }

        public void Subscribe(string topicName, int partition)
        {
            if (_consumerClient == null)
            {
                InitKafkaClient();
            }
            _consumerClient.Assignment.Add(new TopicPartition(topicName, partition));
            _consumerClient.Subscribe(topicName);
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _consumerClient.Poll(timeout);
            }
        }

        public void Commit()
        {
            _consumerClient.CommitAsync();
        }

        public void Dispose()
        {
            _consumerClient.Dispose();
        }

        #region private methods

        private void InitKafkaClient()
        {
            _kafkaOptions.MainConfig.Add("group.id", _groupId);

            var config = _kafkaOptions.AsRdkafkaConfig();
            _consumerClient = new Consumer<Null, string>(config, null, StringDeserializer);

            _consumerClient.OnMessage += ConsumerClient_OnMessage;
        }

        private void ConsumerClient_OnMessage(object sender, Message<Null, string> e)
        {
            var message = new MessageContext
            {
                Group = _groupId,
                Name = e.Topic,
                Content = e.Value
            };
            MessageReceieved?.Invoke(sender, message);
        }

        #endregion private methods
    }
}
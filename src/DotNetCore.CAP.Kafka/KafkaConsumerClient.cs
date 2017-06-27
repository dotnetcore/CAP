using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Kafka
{
    public class KafkaConsumerClient : IConsumerClient
    {
        private readonly string _groupId;
        private readonly KafkaOptions _kafkaOptions;
        private Consumer<Null, string> _consumerClient;

        public event EventHandler<MessageBase> MessageReceieved;

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

        public void Listening(TimeSpan timeout)
        {
            while (true)
            {
                _consumerClient.Poll(timeout);
            }
        }

        public void Dispose()
        {
            _consumerClient.Dispose();
        }

        #region private methods

        private void InitKafkaClient()
        {
            var config = new Dictionary<string, object>{
                { "group.id", _groupId },
                { "bootstrap.servers", _kafkaOptions.Host }
            };

            _consumerClient = new Consumer<Null, string>(config, null, StringDeserializer);
            _consumerClient.OnMessage += ConsumerClient_OnMessage;
        }

        private void ConsumerClient_OnMessage(object sender, Message<Null, string> e)
        {
            var message = new MessageBase
            {
                KeyName = e.Topic,
                Content = e.Value
            };
            MessageReceieved?.Invoke(sender, message);
        }

        #endregion private methods
    }
}
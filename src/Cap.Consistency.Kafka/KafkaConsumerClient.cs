using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cap.Consistency.Consumer;
using Cap.Consistency.Infrastructure;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;

namespace Cap.Consistency.Kafka
{
    public class KafkaConsumerClient : IConsumerClient
    {

        private readonly string _groupId;
        private readonly string _bootstrapServers;

        private Consumer<Null, string> _consumerClient;

        public event EventHandler<DeliverMessage> MessageReceieved;

        public IDeserializer<string> StringDeserializer { get; set; }

        public KafkaConsumerClient(string groupId, string bootstrapServers) {
            _groupId = groupId;
            _bootstrapServers = bootstrapServers;

            StringDeserializer = new StringDeserializer(Encoding.UTF8);
        }

        public void Subscribe(string topic) {
            Subscribe(topic, 0);
        }

        public void Subscribe(string topicName, int partition) {

            if (_consumerClient == null) {
                InitKafkaClient();
            }
            _consumerClient.Assignment.Add(new TopicPartition(topicName, partition));
        }

        public void Listening(TimeSpan timeout) {
            while (true) {
                _consumerClient.Poll(timeout);
            }
        }

        public void Dispose() {
            _consumerClient.Dispose();
        }

        #region private methods

        private void InitKafkaClient() {
            var config = new Dictionary<string, object>{
                { "group.id", _groupId },
                { "bootstrap.servers", _bootstrapServers }
            };

            _consumerClient = new Consumer<Null, string>(config, null, StringDeserializer);
            _consumerClient.OnMessage += ConsumerClient_OnMessage;
        }

        private void ConsumerClient_OnMessage(object sender, Message<Null, string> e) {
            var message = new DeliverMessage {
                MessageKey = e.Topic,
                Value = e.Value
            };
            MessageReceieved?.Invoke(sender, message);
        }
    
        #endregion

    }
}

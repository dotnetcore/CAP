using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Abstractions;

namespace Cap.Consistency.Kafka
{
    public class KafkaTopicAttribute : TopicAttribute
    {
        public KafkaTopicAttribute(string topicName)
            : this(topicName, 0) { }

        public KafkaTopicAttribute(string topicName, int partition)
            : this(topicName, partition, 0) { }

        public KafkaTopicAttribute(string topicName, int partition, long offset)
            : base(topicName) {
            Offset = offset;
            Partition = partition;
        }

        public int Partition { get; }

        public long Offset { get; }

        public bool IsPartition { get { return Partition == 0; } }

        public bool IsOffset { get { return Offset == 0; } }

        public override string ToString() {
            return Name;
        }
    }
}

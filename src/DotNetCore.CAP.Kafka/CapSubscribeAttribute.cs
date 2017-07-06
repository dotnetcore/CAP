using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.Kafka
{
    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string topicName)
            : this(topicName, 0) { }

        /// <summary>
        /// Not support
        /// </summary>
        public CapSubscribeAttribute(string topicName, int partition)
            : this(topicName, partition, 0) { }

        /// <summary>
        /// Not support
        /// </summary>
        public CapSubscribeAttribute(string topicName, int partition, long offset)
            : base(topicName)
        {
            Offset = offset;
            Partition = partition;
        }

        public int Partition { get; }

        public long Offset { get; }

        public bool IsPartition { get { return Partition == 0; } }

        public bool IsOffset { get { return Offset == 0; } }

        public override string ToString()
        {
            return Name;
        }
    }
}
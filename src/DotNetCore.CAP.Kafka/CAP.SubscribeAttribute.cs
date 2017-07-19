using DotNetCore.CAP.Abstractions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string name)
            : this(name, 0)
        {
        }

        /// <summary>
        /// Not support
        /// </summary>
        public CapSubscribeAttribute(string name, int partition)
            : this(name, partition, 0)
        {
        }

        /// <summary>
        /// Not support
        /// </summary>
        public CapSubscribeAttribute(string name, int partition, long offset)
            : base(name)
        {
            Offset = offset;
            Partition = partition;
        }

        public int Partition { get; }

        public long Offset { get; }

        public bool IsPartition => Partition == 0;

        public bool IsOffset => Offset == 0;

        public override string ToString()
        {
            return Name;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Cap.Consistency
{
    public class TopicInfo
    {
        public TopicInfo(string topicName) : this(topicName, 0) {}

        public TopicInfo(string topicName, int partition) : this(topicName, partition, 0) {}

        public TopicInfo(string topicName, int partition, long offset) {
            Name = topicName;
            Offset = offset;
            Partition = partition;       
        }       

        public string Name { get; }
    
        public int Partition { get; }

        public long Offset { get; }

        public bool IsPartition { get { return Partition == 0; } }

        public bool IsOffset { get { return Offset == 0; } }

        public override string ToString() {
            return Name;
        }

    }
}

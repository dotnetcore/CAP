using System;

namespace Cap.Consistency.Test
{
    public class TestConsistencyMessage : TestConsistencyMessage<string>
    {
        public TestConsistencyMessage() {
            Id = Guid.NewGuid().ToString();
        }
    }


    public class TestConsistencyMessage<TKey> where TKey : IEquatable<TKey>
    {
        public TestConsistencyMessage() { }

        public virtual TKey Id { get; set; }

        public virtual DateTime Time { get; set; }
    }

}

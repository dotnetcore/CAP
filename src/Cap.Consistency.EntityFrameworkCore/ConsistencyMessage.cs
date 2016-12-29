using System;

namespace Cap.Consistency.EntityFrameworkCore
{
    /// <summary>
    /// The default implementation of <see cref="ConsistencyMessage{TKey}"/> which uses a string as a primary key.
    /// </summary>
    public class ConsistencyMessage : ConsistencyMessage<string>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ConsistencyMessage"/>.
        /// </summary>
        /// <remarks>
        /// The Id property is initialized to from a new GUID string value.
        /// </remarks>
        public ConsistencyMessage() {
            Id = Guid.NewGuid().ToString();
            SendTime = DateTime.Now;
            UpdateTime = SendTime;
            Status = MessageStatus.WaitForSend;
        }
    }

    /// <summary>
    /// ConsistencyMessage consume status
    /// </summary>
    public enum MessageStatus
    {
        Deleted = 0,
        WaitForSend = 1,
        RollbackSuccessed = 3,
        RollbackFailed = 4
    }

    /// <summary>
    /// Represents a message in the consistency system
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the message.</typeparam>
    public class ConsistencyMessage<TKey> where TKey : IEquatable<TKey>
    {
        public virtual TKey Id { get; set; }

        public virtual DateTime SendTime { get; set; }

        public string Payload { get; set; }

        public MessageStatus Status { get; set; }

        public virtual DateTime? UpdateTime { get; set; }
    }
}
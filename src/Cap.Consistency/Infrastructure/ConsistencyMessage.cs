using System;

namespace Cap.Consistency.Infrastructure
{
    /// <summary>
    /// The default implementation of <see cref="ConsistencyMessage{TKey}"/> which uses a string as a primary key.
    /// </summary>
    public class ConsistencyMessage 
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

        public string Id { get; set; }

        public DateTime SendTime { get; set; }

        public string Payload { get; set; }

        public MessageStatus Status { get; set; }

        public virtual DateTime? UpdateTime { get; set; }
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
}
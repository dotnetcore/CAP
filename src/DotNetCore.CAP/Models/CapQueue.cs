namespace DotNetCore.CAP.Models
{
    public class CapQueue
    {
        public int MessageId { get; set; }

        /// <summary>
        /// 0 is CapSentMessage, 1 is CapReceivedMessage
        /// </summary>
        public MessageType MessageType { get; set; }
    }
}
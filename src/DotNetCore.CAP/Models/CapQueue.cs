namespace DotNetCore.CAP.Models
{
    public class CapQueue
    {
        public int Id { get; set; }

        public string MessageId { get; set; }

        /// <summary>
        /// 0 is CapSentMessage, 1 is CapReceviedMessage
        /// </summary>
        public MessageType Type { get; set; }
    }
}

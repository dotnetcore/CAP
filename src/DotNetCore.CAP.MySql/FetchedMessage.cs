using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.MySql
{
    public class FetchedMessage
    {
        public int MessageId { get; set; }

        public MessageType MessageType { get; set; }
    }
}
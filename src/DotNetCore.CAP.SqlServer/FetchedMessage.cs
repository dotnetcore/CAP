using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.SqlServer
{
    public class FetchedMessage
    {
        public int MessageId { get; set; }

        public MessageType MessageType { get; set; }
    }
}
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.SqlServer
{
    internal class FetchedMessage
    {
        public int MessageId { get; set; }

        public MessageType MessageType { get; set; }
    }
}
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.PostgreSql
{
    internal class FetchedMessage
    {
        public int MessageId { get; set; }

        public MessageType MessageType { get; set; }
    }
}
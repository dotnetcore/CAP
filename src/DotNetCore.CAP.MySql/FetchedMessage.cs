using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.MySql
{
    internal class FetchedMessage
    {
        public int MessageId { get; set; }

        public MessageType MessageType { get; set; }
    }
}
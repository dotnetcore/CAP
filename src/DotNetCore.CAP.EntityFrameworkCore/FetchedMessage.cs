using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class FetchedMessage
    {
        public int MessageId { get; set; }

        public MessageType MessageType { get; set; }
    }
}
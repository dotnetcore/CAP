using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.MongoDB
{
    internal class ReceivedMessage : CapReceivedMessage
    {
        public string Version { get; set; }
    }

    internal class PublishedMessage : CapPublishedMessage
    {
        public string Version { get; set; }
    }
}

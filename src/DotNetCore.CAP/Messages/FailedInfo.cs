using System;

namespace DotNetCore.CAP.Messages
{
    public class FailedInfo
    {
        public IServiceProvider ServiceProvider { get; set; }

        public MessageType MessageType { get; set; }

        public Message Message { get; set; }
    }
}

using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Persistence
{
    public class MediumMessage
    {
        public string DbId { get; set; }

        public Message Origin { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }
    }
}

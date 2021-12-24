using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Persistence
{
    public class MediumMessage
    {
        public string DbId { get; set; } = default!;

        public Message Origin { get; set; } = default!;

        public string Content { get; set; } = default!;

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }
    }
}

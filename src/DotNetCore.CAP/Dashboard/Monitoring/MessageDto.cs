using System;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class MessageDto
    {
        public int Id { get; set; }

        public string Group { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; }
    }
}
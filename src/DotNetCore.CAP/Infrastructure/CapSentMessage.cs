using System;

namespace DotNetCore.CAP.Infrastructure
{
    public class CapSentMessage 
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CapSentMessage"/>.
        /// </summary>
        /// <remarks>
        /// The Id property is initialized to from a new GUID string value.
        /// </remarks>
        public CapSentMessage()
        {
            Id = Guid.NewGuid().ToString();
            Added = DateTime.Now;
        }

        public CapSentMessage(MessageContext message)
        {
            KeyName = message.KeyName;
            Content = message.Content;
        }

        public string Id { get; set; } 

        public string KeyName { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime LastRun { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; }
    }
}
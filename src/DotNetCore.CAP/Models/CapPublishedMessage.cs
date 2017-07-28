using System;

namespace DotNetCore.CAP.Models
{
    public class CapPublishedMessage
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CapPublishedMessage"/>.
        /// </summary>
        /// <remarks>
        /// The Id property is initialized to from a new GUID string value.
        /// </remarks>
        public CapPublishedMessage()
        {
            Added = DateTime.Now;
        }

        public CapPublishedMessage(MessageContext message)
        {
            Name = message.Name;
            Content = message.Content;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; }
    }
}
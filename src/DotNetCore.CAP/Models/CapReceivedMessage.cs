using System;

namespace DotNetCore.CAP.Models
{
    public class CapReceivedMessage
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CapReceivedMessage"/>.
        /// </summary>
        /// <remarks>
        /// The Id property is initialized to from a new GUID string value.
        /// </remarks>
        public CapReceivedMessage()
        {
            Added = DateTime.Now;
        }

        public CapReceivedMessage(MessageContext message) : this()
        {
            Group = message.Group;
            Name = message.Name;
            Content = message.Content;
        }

        public int Id { get; set; }

        public string Group { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; }

        public MessageContext ToMessageContext()
        {
            return new MessageContext
            {
                Group = Group,
                Name = Name,
                Content = Content
            };
        }
    }
}
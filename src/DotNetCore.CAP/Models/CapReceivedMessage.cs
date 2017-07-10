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
            Id = Guid.NewGuid().ToString();
            Added = DateTime.Now;
        }

        public CapReceivedMessage(MessageContext message) : this()
        {
            Group = message.Group;
            KeyName = message.KeyName;
            Content = message.Content;
        }

        public string Id { get; set; }

        public string Group { get; set; }

        public string KeyName { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime LastRun { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; }

        public MessageContext ToMessageContext()
        {
            return new MessageContext
            {
                Group = Group,
                KeyName = KeyName,
                Content = Content
            };
        }
    }
}
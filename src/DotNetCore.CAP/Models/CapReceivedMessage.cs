// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Models
{
    public class CapReceivedMessage
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CapReceivedMessage" />.
        /// </summary>
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

        public long Id { get; set; }

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

        public override string ToString()
        {
            return "name:" + Name + ", group:" + Group + ", content:" + Content;
        }
    }
}
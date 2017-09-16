using System;

namespace DotNetCore.CAP.Models
{
    public class Message
    {
        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public object Content { get; set; }

        public string CallbackName { get; set; }

        public Message()
        {
            Id = ObjectId.GenerateNewStringId();
            Timestamp = DateTime.Now;
        }

        public Message(object content) : this()
        {
            Content = content;
        }
    }
}
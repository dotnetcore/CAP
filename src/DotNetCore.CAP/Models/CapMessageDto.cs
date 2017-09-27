using System;

namespace DotNetCore.CAP.Models
{
    public class CapMessageDto
    {
        public virtual string Id { get; set; }

        public virtual DateTime Timestamp { get; set; }

        public virtual object Content { get; set; }

        public virtual string CallbackName { get; set; }

        public CapMessageDto()
        {
            Id = ObjectId.GenerateNewStringId();
            Timestamp = DateTime.Now;
        }

        public CapMessageDto(object content) : this()
        {
            Content = content;
        }
    }
}
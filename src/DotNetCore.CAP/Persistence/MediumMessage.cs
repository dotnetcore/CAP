using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Persistence
{
    public class MediumMessage : IMediumMessage
    {

        public string DbId { get; set; }

        public ICapMessage Origin
        {
            get;
            set;
        }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }
    }

    public class MediumMessage<T> : IMediumMessage
    {
        public string DbId { get; set; }

        public ICapMessage Origin { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }
    }
}

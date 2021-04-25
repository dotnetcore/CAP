using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Messages
{
    public interface IMediumMessage
    {
        public string DbId { get; set; }

        public ICapMessage Origin { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }
    }

    public interface IMediumMessage<T>
    {
        public string DbId { get; set; }

        public ICapMessage Origin { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }

    }

    public static class MediumMessageExtensions
    {
        public static Type GetOriginType(this IMediumMessage medium)
        {
            return medium.Origin.GetMessageType();
        }

        public static Type GetOriginType<T>(this IMediumMessage<T> medium)
        {
            return medium.Origin.GetMessageType();
        }
    }
}

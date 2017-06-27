using System;

namespace DotNetCore.CAP.Infrastructure
{
    /// <summary>
    /// cap messages store model.
    /// </summary>
    public abstract class CapMessage : MessageBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CapMessage"/>.
        /// </summary>
        /// <remarks>
        /// The Id property is initialized to from a new GUID string value.
        /// </remarks>
        public CapMessage()
        {
            Id = Guid.NewGuid().ToString();
            Added = DateTime.Now;
        }

        public CapMessage(MessageBase message)
        {
            KeyName = message.KeyName;
            Content = message.Content;
        }

        public string Id { get; set; }

        public DateTime Added { get; set; }

        public DateTime LastRun { get; set; }

        public int Retries { get; set; }

        public string StateName { get; set; }
    }

    /// <summary>
    /// The message state name.
    /// </summary>
    public struct StateName
    {
        public const string Enqueued = nameof(Enqueued);
        public const string Processing = nameof(Processing);
        public const string Succeeded = nameof(Succeeded);
        public const string Failed = nameof(Failed);
    }
}
using System;
using System.Collections.Generic;

namespace DotNetCore.CAP.Messages
{
    /// <summary>
    /// Message content field
    /// </summary>
    public class TransportMessage
    {
        public TransportMessage(IDictionary<string, string> headers, byte[] body)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary>
        /// Gets the headers of this message
        /// </summary>
        public IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the body object of this message
        /// </summary>
        public byte[] Body { get; }

        public string GetName()
        {
            return Headers.TryGetValue(Messages.Headers.MessageName, out var value) ? value : null;
        }

        public string GetGroup()
        {
            return Headers.TryGetValue(Messages.Headers.Group, out var value) ? value : null;
        }
    }
}

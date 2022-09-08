// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace DotNetCore.CAP.Messages
{
    /// <summary>
    /// Message content field
    /// </summary>
    [Serializable]
    public class TransportMessage
    {
        public TransportMessage(IDictionary<string, string?> headers, byte[]? body)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Body = body;
        }

        /// <summary>
        /// Gets the headers of this message
        /// </summary>
        public IDictionary<string, string?> Headers { get; }

        /// <summary>
        /// Gets the body object of this message
        /// </summary>
        public byte[]? Body { get; }

        public string GetId()
        {
            return Headers.TryGetValue(Messages.Headers.MessageId, out var value) ? value! : "Unknown";
        }

        public string GetName()
        {
            return Headers.TryGetValue(Messages.Headers.MessageName, out var value) ? value! : "";
        }

        public string? GetGroup()
        {
            return Headers.TryGetValue(Messages.Headers.Group, out var value) ? value : null;
        }
        
        public string? GetCorrelationId()
        {
            return Headers.TryGetValue(Messages.Headers.CorrelationId, out var value) ? value : null;
        }
    }
}

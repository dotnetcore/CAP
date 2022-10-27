// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DotNetCore.CAP.Messages;

/// <summary>
/// Represents message entry
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct TransportMessage
{
    public TransportMessage(IDictionary<string, string?> headers, ReadOnlyMemory<byte> body)
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
    public ReadOnlyMemory<byte> Body { get; }

    public string GetId()
    {
        return Headers[Messages.Headers.MessageId]!;
    }

    public string GetName()
    {
        return Headers[Messages.Headers.MessageName]!;
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
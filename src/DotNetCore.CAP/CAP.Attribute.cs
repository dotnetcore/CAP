// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP;

/// <summary>
/// Represents an attribute that is applied to a method to indicate that it is a subscriber for a specific CAP topic.
/// </summary>
public class CapSubscribeAttribute : TopicAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CapSubscribeAttribute"/> class with the specified topic name and partial flag.
    /// </summary>
    /// <param name="name">The name of the CAP topic.</param>
    /// <param name="isPartial">A flag indicating whether the subscriber is a partial subscriber.</param>
    public CapSubscribeAttribute(string name, bool isPartial = false)
        : base(name, isPartial)
    {
    }

    /// <summary>
    /// Returns a string that represents the current <see cref="CapSubscribeAttribute"/>.
    /// </summary>
    /// <returns>A string that represents the current <see cref="CapSubscribeAttribute"/>.</returns>
    public override string ToString()
    {
        return Name;
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class FromCapAttribute : Attribute
{
}

public class CapHeader : ReadOnlyDictionary<string, string?>
{
    internal IDictionary<string, string?>? ResponseHeader { get; set; }

    public CapHeader(IDictionary<string, string?> dictionary) : base(dictionary)
    {
    }

    /// <summary>
    /// When a callbackName is specified from publish message, use this method to add an additional header.
    /// </summary>
    /// <param name="key">The response header key.</param>
    /// <param name="value">The response header value.</param>
    public void AddResponseHeader(string key, string? value)
    {
        ResponseHeader ??= new Dictionary<string, string?>();
        ResponseHeader[key] = value;
    }

    /// <summary>
    /// When a callbackName is specified from publish message, use this method to abort the callback.
    /// </summary>
    public void RemoveCallback()
    {
        Dictionary.Remove(Headers.CallbackName);
    }

    /// <summary>
    /// When a callbackName is specified from Publish message, use this method to rewrite the callback name.
    /// </summary>
    /// <param name="callbackName">The new callback name.</param>
    public void RewriteCallback(string callbackName)
    {
        Dictionary[Headers.CallbackName] = callbackName;
    }
}

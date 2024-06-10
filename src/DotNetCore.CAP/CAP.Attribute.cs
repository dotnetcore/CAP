// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP;

public class CapSubscribeAttribute : TopicAttribute
{
    public CapSubscribeAttribute(string name, bool isPartial = false)
        : base(name, isPartial)
    {
    }

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
    /// <param name="callbackName"></param>
    public void RewriteCallback(string callbackName)
    {
        Dictionary[Headers.CallbackName] = callbackName;
    }
}
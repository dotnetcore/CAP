﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Internal;

/// <summary>
/// A context for consumers, it used to be provider wrapper of method description and received message.
/// </summary>
public class ConsumerContext
{
    public ConsumerContext(ConsumerContext context)
        : this(context.ConsumerDescriptor, context.MediumMessage)
    {
    }

    /// <summary>
    /// create a new instance of  <see cref="ConsumerContext" /> .
    /// </summary>
    /// <param name="descriptor">consumer method descriptor. </param>
    /// <param name="message"> received message.</param>
    public ConsumerContext(ConsumerExecutorDescriptor descriptor, MediumMessage message)
    {
        ConsumerDescriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        MediumMessage = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>
    /// a descriptor of consumer information need to be performed.
    /// </summary>
    public ConsumerExecutorDescriptor ConsumerDescriptor { get; }

    /// <summary>
    /// consumer received message.
    /// </summary>
    public Message DeliverMessage => MediumMessage.Origin;

    /// <summary>
    /// consumer received medium message.
    /// </summary>
    public MediumMessage MediumMessage { get; }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using NATS.Client;
using NATS.Client.JetStream;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP;

/// <summary>
/// Provides programmatic configuration for the CAP NATS project.
/// </summary>
public class NATSOptions
{
    /// <summary>
    /// Gets or sets the server url/urls used to connect to the NATs server.
    /// </summary>
    /// <remarks>This may contain username/password information.</remarks>
    public string Servers { get; set; } = "nats://127.0.0.1:4222";

    /// <summary>
    /// connection pool size, default is 10
    /// </summary>
    public int ConnectionPoolSize { get; set; } = 10;

    /// <summary>
    /// Allows a nats consumer client to dynamically create a stream and configure the expected subjects on the stream. Defaults to true.
    /// </summary>
    public bool EnableSubscriberClientStreamAndSubjectCreation { get; set; } = true;

    /// <summary>
    /// Used to setup all NATs client options
    /// </summary>
    public Options? Options { get; set; }

    public Action<StreamConfiguration.StreamConfigurationBuilder>? StreamOptions { get; set; }

    public Action<ConsumerConfiguration.ConsumerConfigurationBuilder>? ConsumerOptions { get; set; }

    /// <summary>
    /// If you need to get additional native delivery args, you can use this function to write into <see cref="CapHeader" />.
    /// </summary>
    public Func<MsgHandlerEventArgs, IServiceProvider, List<KeyValuePair<string, string>>>? CustomHeadersBuilder { get; set; }

    public Func<string, string> NormalizeStreamName { get; set; } = origin => origin.Split('.')[0];
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using NATS.Client;
using NATS.Client.JetStream;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides programmatic configuration for the CAP NATS project.
    /// </summary>
    public class NATSOptions
    {
        /// <summary>
        /// Gets or sets the server url/urls used to connect to the NATs server.
        /// </summary>
        /// <remarks>This may contain username/password information.</remarks>
        public string Servers { get; set; } = "nats://localhost:4222";

        /// <summary>
        /// connection pool size, default is 10
        /// </summary>
        public int ConnectionPoolSize { get; set; } = 10;

        /// <summary>
        /// Used to setup all NATs client options
        /// </summary>
        public Options? Options { get; set; }

        public Action<StreamConfiguration.StreamConfigurationBuilder>? StreamOptions { get; set; }

        public Func<string, string> NormalizeStreamName { get; set; } = origin => origin.Split('.')[0];
    }
}
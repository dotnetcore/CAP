// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace

using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class RabbitMQOptions
    {
        /// <summary>
        /// Default password (value: "guest").
        /// </summary>
        /// <remarks>PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const string DefaultPass = "guest";

        /// <summary>
        /// Default user name (value: "guest").
        /// </summary>
        /// <remarks>PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const string DefaultUser = "guest";

        /// <summary>
        /// Default virtual host (value: "/").
        /// </summary>
        /// <remarks> PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const string DefaultVHost = "/";

        /// <summary>
        /// Default exchange name (value: "cap.default.router").
        /// </summary>
        public const string DefaultExchangeName = "cap.default.router";

        /// <summary> The topic exchange type. </summary>
        public const string ExchangeType = "topic";

        /// <summary>
        /// The host to connect to.
        /// If you want connect to the cluster, you can assign like “192.168.1.111,192.168.1.112”
        /// </summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// Password to use when authenticating to the server.
        /// </summary>
        public string Password { get; set; } = DefaultPass;

        /// <summary>
        /// Username to use when authenticating to the server.
        /// </summary>
        public string UserName { get; set; } = DefaultUser;

        /// <summary>
        /// Virtual host to access during this connection.
        /// </summary>
        public string VirtualHost { get; set; } = DefaultVHost;

        /// <summary>
        /// Topic exchange name when declare a topic exchange.
        /// </summary>
        public string ExchangeName { get; set; } = DefaultExchangeName;

        /// <summary>
        /// The port to connect on.
        /// </summary>
        public int Port { get; set; } = -1;

        /// <summary>
        /// Optional queue arguments, also known as "x-arguments" because of their field name in the AMQP 0-9-1 protocol,
        /// is a map (dictionary) of arbitrary key/value pairs that can be provided by clients when a queue is declared.
        /// </summary>
        public QueueArgumentsOptions QueueArguments { get; set; } = new ();

        /// <summary>
        /// If you need to get additional native delivery args, you can use this function to write into <see cref="CapHeader"/>.
        /// </summary>
        public Func<BasicDeliverEventArgs, List<KeyValuePair<string, string>>>? CustomHeaders { get; set; }

        /// <summary>
        /// RabbitMQ native connection factory options
        /// </summary>
        public Action<ConnectionFactory>? ConnectionFactoryOptions { get; set; }

        public class QueueArgumentsOptions
        {
            /// <summary>
            /// Gets or sets queue mode by supplying the 'x-queue-mode' declaration argument with a string specifying the desired mode.
            /// </summary>
            public string QueueMode { get; set; } = default!;

            /// <summary>
            /// Gets or sets queue message automatic deletion time (in milliseconds) "x-message-ttl", Default 864000000 ms (10 days).
            /// </summary>
            // ReSharper disable once InconsistentNaming
            public int MessageTTL { get; set; } = 864000000;
        }
    }
}
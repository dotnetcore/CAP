// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
        /// Enabling Publisher Confirms on a Channel
        /// </summary>
        public bool PublishConfirms { get; set; }

        /// <summary>
        /// The port to connect on.
        /// </summary>
        public int Port { get; set; } = -1;

        /// <summary>
        /// Optional queue arguments, also known as "x-arguments" because of their field name in the AMQP 0-9-1 protocol,
        /// is a map (dictionary) of arbitrary key/value pairs that can be provided by clients when a queue is declared.
        /// </summary>
        public QueueArgumentsOptions QueueArguments { get; set; } = new();

        /// <summary>
        /// If you need to get additional native delivery args, you can use this function to write into <see cref="CapHeader"/>.
        /// </summary>
        public Func<BasicDeliverEventArgs, List<KeyValuePair<string, string>>>? CustomHeaders { get; set; }

        /// <summary>
        /// RabbitMQ native connection factory options
        /// </summary>
        public Action<ConnectionFactory>? ConnectionFactoryOptions { get; set; }

        /// <summary> 
        /// Specify quality of service.
        /// <br/><br/>
        /// This settings requests a specific quality of service.The QoS can be specified for the current channel or for all channels on the connection.<br/>
        /// The particular properties and semantics of a qos method always depend on the content class semantics.<br/>
        /// Though the qos method could in principle apply to both peers, it is currently meaningful only for the server.<br/>
        /// <br/>
        /// <see href="https://www.rabbitmq.com/consumer-prefetch.html">More info at: https://www.rabbitmq.com/consumer-prefetch.html</see>
        /// </summary>
        public BasicQos? BasicQosOptions { get; set; } = null;

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

            /// <summary>
            /// Gets or sets queue type by supplying the 'x-queue-type' declaration argument with a string specifying the desired type.
            /// </summary>
            public string QueueType { get; set; } = default!;
        }
                
        public class BasicQos
        {       
            /// <summary>
            /// New instance of BasicQos sets the use of basic qos setup on the basic channel.
            /// </summary>            
            /// <param name="prefetchCount">Sets the PrefetchCount.</param>
            /// <param name="global">Sets Global flag (default false).</param>
            public BasicQos(ushort prefetchCount, bool global = false)
            {
                PrefetchCount = prefetchCount;
                Global = global;
            }   

            /// <summary>
            /// Gets the PrefetchCount, a value of 0 is treated as infinite, allowing any number of unacknowledged message being pushed to consumer.
            /// The default value is 0.
            /// </summary>
            public ushort PrefetchCount { get; }

            /// <summary>
            /// Gets the global flag across all consumers in RabbitMq.
            /// false (default) - applied separately to each new consumer on the channel
            /// true - shared across all consumers on the channel
            /// </summary>
            public bool Global { get; }
        }
    }
}
// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace

using DotNetCore.CAP.ZeroMQ;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class ZeroMQOptions
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

        public NetMQPattern Pattern { get; set; }

        /// <summary>
        /// The port to connect on.
        /// </summary>
        public int PubPort { get; set; } = 5557;

        public int SubPort { get; set; } = 5556;

        /// <summary>
        /// Gets or sets queue message automatic deletion time (in milliseconds). Default 864000000 ms (10 days).
        /// </summary>
        public int QueueMessageExpires { get; set; } = 864000000;

        /// <summary>
        /// ZeroMQ native connection factory options
        /// </summary>
    }
}
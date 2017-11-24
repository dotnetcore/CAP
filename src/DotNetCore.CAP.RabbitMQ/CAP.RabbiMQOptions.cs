﻿// ReSharper disable once CheckNamespace

namespace DotNetCore.CAP
{
    public class RabbitMQOptions
    {
        /// <summary>
        /// Default value for connection attempt timeout, in milliseconds.
        /// </summary>
        public const int DefaultConnectionTimeout = 30 * 1000;

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
        /// Default exchange name (value: "cap.default.topic").
        /// </summary>
        public const string DefaultExchangeName = "cap.default.exchange";

        /// <summary> The topic exchange type. </summary>
        public const string ExchangeType = "topic";

        /// <summary>The host to connect to.</summary>
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
        public string TopicExchangeName { get; set; } = DefaultExchangeName;

        /// <summary>
        /// Timeout setting for connection attempts (in milliseconds).
        /// </summary>
        public int RequestedConnectionTimeout { get; set; } = DefaultConnectionTimeout;

        /// <summary>
        /// Timeout setting for socket read operations (in milliseconds).
        /// </summary>
        public int SocketReadTimeout { get; set; } = DefaultConnectionTimeout;

        /// <summary>
        /// Timeout setting for socket write operations (in milliseconds).
        /// </summary>
        public int SocketWriteTimeout { get; set; } = DefaultConnectionTimeout;

        /// <summary>
        /// The port to connect on.
        /// </summary>
        public int Port { get; set; } = -1;

        /// <summary>
        /// Gets or sets queue message automatic deletion time (in milliseconds). Default 864000000 ms (10 days).
        /// </summary>
        public int QueueMessageExpires { get; set; } = 864000000;
    }
}
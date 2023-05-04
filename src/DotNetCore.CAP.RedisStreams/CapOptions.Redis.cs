// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using StackExchange.Redis;
using System.Linq;
using System.Net;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class CapRedisOptions
    {
        private ConfigurationOptions? _configuration;

        /// <summary>
        ///     Gets or sets the native options of StackExchange.Redis
        /// </summary>
        public ConfigurationOptions? Configuration
        {
            get => _configuration;
            set
            {
                _configuration = value;

                Endpoint = _configuration == null
                    ? null
                    : string.Join(";", _configuration.EndPoints.Select(e =>
                    {
                        if (e is IPEndPoint ie) return $"{ie.Address}:{ie.Port}";

                        if (e is DnsEndPoint de) return $"{de.Host}:{de.Port}";

                        return null;
                    }).Where(e => e != null));
            }
        }

        internal string? Endpoint { get; set; }

        /// <summary>
        ///     Gets or sets the count of entries consumed from stream
        /// </summary>
        public uint StreamEntriesCount { get; set; }

        /// <summary>
        ///     Gets or sets the number of connections that can be used with redis server
        /// </summary>
        public uint ConnectionPoolSize { get; set; }
    }
}
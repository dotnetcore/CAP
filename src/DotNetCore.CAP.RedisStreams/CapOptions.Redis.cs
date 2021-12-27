// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class CapRedisOptions
    {
        /// <summary>
        ///     Gets or sets the native options of StackExchange.Redis
        /// </summary>
        public ConfigurationOptions? Configuration { get; set; }

        internal string Endpoint { get; set; } = default!;

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
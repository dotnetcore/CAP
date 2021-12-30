// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;
using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapRedisOptionsExtensions
    {
        public static CapOptions UseRedis(this CapOptions options)
        {
            return options.UseRedis(_ => { });
        }

        /// <summary>
        /// Use redis streams as the message transport.
        /// </summary>
        /// <param name="options">The <see cref="CapOptions"/>.</param>
        /// <param name="connection">The StackExchange.Redis <see cref="ConfigurationOptions"/> comma-delimited configuration string.</param>
        public static CapOptions UseRedis(this CapOptions options, string connection)
        {
            return options.UseRedis(opt => opt.Configuration = ConfigurationOptions.Parse(connection));
        }

        /// <summary>
        /// Use redis streams as the message transport.
        /// </summary>
        /// <param name="options">The <see cref="CapOptions"/>.</param>
        /// <param name="configure">The CAP redis client options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
        public static CapOptions UseRedis(this CapOptions options, Action<CapRedisOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            options.RegisterExtension(new RedisOptionsExtension(configure));

            return options;
        }
    }
}
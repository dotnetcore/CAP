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

        public static CapOptions UseRedis(this CapOptions options, string connection)
        {
            return options.UseRedis(opt => opt.Configuration = ConfigurationOptions.Parse(connection));
        }


        public static CapOptions UseRedis(this CapOptions options, Action<CapRedisOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            options.RegisterExtension(new RedisOptionsExtension(configure));

            return options;
        }
    }
}
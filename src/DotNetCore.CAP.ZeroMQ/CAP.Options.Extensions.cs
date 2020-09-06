// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP;
using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseZeroMQ(this CapOptions options, string hostName)
        {
            return options.UseZeroMQ(opt => { opt.HostName = hostName; });
        }

        public static CapOptions UseZeroMQ(this CapOptions options, Action<ZeroMQOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            options.RegisterExtension(new ZeroMQCapOptionsExtension(configure));

            return options;
        }
    }
}
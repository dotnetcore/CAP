// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        /// <summary>
        /// Configuration to use NATS in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="bootstrapServers">NATS bootstrap server urls.</param>
        public static CapOptions UseNATS(this CapOptions options, string? bootstrapServers = null)
        {
            return options.UseNATS(opt =>
            {
                if (bootstrapServers != null)
                    opt.Servers = bootstrapServers;
            });
        }

        /// <summary>
        /// Configuration to use NATS in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="configure">Provides programmatic configuration for the NATS.</param>
        /// <returns></returns>
        public static CapOptions UseNATS(this CapOptions options, Action<NATSOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            options.RegisterExtension(new NATSCapOptionsExtension(configure));

            return options;
        }
    }
}
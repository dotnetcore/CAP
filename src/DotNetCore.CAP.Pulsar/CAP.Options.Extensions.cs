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
        /// Configuration to use pulsar in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="serverUrl">Pulsar bootstrap server urls.</param>
        public static CapOptions UsePulsar(this CapOptions options, string serverUrl)
        {
            return options.UsePulsar(opt => { opt.ServiceUrl = serverUrl; });
        }

        /// <summary>
        /// Configuration to use pulsar in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="configure">Provides programmatic configuration for the pulsar .</param>
        /// <returns></returns>
        public static CapOptions UsePulsar(this CapOptions options, Action<PulsarOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            options.RegisterExtension(new PulsarCapOptionsExtension(configure));

            return options;
        }
    }
}
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
        /// Configuration to use Azure Service Bus in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="connectionString">Connection string for namespace or the entity.</param>
        public static CapOptions UseAzureServiceBus(this CapOptions options, string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            return options.UseAzureServiceBus(opt => { opt.ConnectionString = connectionString; });
        }

        /// <summary>
        /// Configuration to use Azure Service Bus in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="configure">Provides programmatic configuration for the Azure Service Bus.</param>
        public static CapOptions UseAzureServiceBus(this CapOptions options, Action<AzureServiceBusOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            options.RegisterExtension(new AzureServiceBusOptionsExtension(configure));

            return options;
        }
    }
}
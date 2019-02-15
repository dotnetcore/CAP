// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;
using Microsoft.Azure.ServiceBus;

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

            options.RegisterExtension(new AzureServiceBusOptionsExtension(x => x.ConnectionString = connectionString));

            return options;
        }

        /// <summary>
        /// Configuration to use Azure Service Bus in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="connectionStringBuilder">Provides programmatic configuration for the Azure Service Bus.</param>
        public static CapOptions UseAzureServiceBus(this CapOptions options, ServiceBusConnectionStringBuilder connectionStringBuilder)
        {
            if (connectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(connectionStringBuilder));
            }

            options.RegisterExtension(new AzureServiceBusOptionsExtension(x => x.ConnectionStringBuilder = connectionStringBuilder));

            return options;
        }
    }
}
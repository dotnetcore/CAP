// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides programmatic configuration for the CAP Azure Service Bus project.
    /// </summary>
    public class AzureServiceBusOptions
    {
        public int ConnectionPoolSize { get; set; } = 10;

        /// <summary>
        /// Azure Service Bus Namespace connection string. Must not contain topic information.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string TopicPath { get; set; } = "topic";

        /// <summary>
        /// Used to generate Service Bus connection strings
        /// </summary>
        public ServiceBusConnectionStringBuilder ConnectionStringBuilder { get; set; }
    }
}
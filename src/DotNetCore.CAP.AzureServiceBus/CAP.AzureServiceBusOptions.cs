﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.AzureServiceBus;
using Microsoft.Azure.ServiceBus.Primitives;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides programmatic configuration for the CAP Azure Service Bus project.
    /// </summary>
    public class AzureServiceBusOptions
    {
        /// <summary>
        /// TopicPath default value for CAP.
        /// </summary>
        public const string DefaultTopicPath = "cap";

        /// <summary>
        /// Azure Service Bus Namespace connection string. Must not contain topic information.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Whether Service Bus sessions are enabled. If enabled, all messages must contain a
        /// <see cref="AzureServiceBusHeaders.SessionId"/> header. Defaults to false.
        /// </summary>
        public bool EnableSessions { get; set; } = false;

        /// <summary>
        /// The name of the topic relative to the service namespace base address.
        /// </summary>
        public string TopicPath { get; set; } = DefaultTopicPath;

        /// <summary>
        /// Represents the Azure Active Directory token provider for Azure Managed Service Identity integration.
        /// </summary>
        public ITokenProvider ManagementTokenProvider { get; set; } 
    }
}
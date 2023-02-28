// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using DotNetCore.CAP.AzureServiceBus;
using DotNetCore.CAP.AzureServiceBus.Producer;

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
        public string ConnectionString { get; set; } = default!;

        /// <summary>
        /// Namespace of service bus , Needs to be set when using with TokenCredential Property
        /// </summary>
        public string Namespace { get; set; } = default!;

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
        /// Gets a value that indicates whether the processor should automatically complete messages after the message handler has completed processing.
        /// If the message handler triggers an exception, the message will not be automatically completed.
        /// </summary>
        public bool AutoCompleteMessages { get; set; }

        /// <summary>
        /// Gets the maximum number of concurrent calls to the ProcessMessageAsync message handler the processor should initiate.
        /// </summary>
        public int MaxConcurrentCalls { get; set; }

        /// <summary>
        /// Represents the Azure Active Directory token provider for Azure Managed Service Identity integration.
        /// </summary>
        public Azure.Core.TokenCredential? TokenCredential { get; set; }

        /// <summary>
        /// Use this function to write additional headers from the original ASB Message or any Custom Header, i.e. to allow compatibility with heterogeneous systems, into <see cref="CapHeader"/>
        /// </summary>
        public Func<ServiceBusReceivedMessage, List<KeyValuePair<string, string>>>? CustomHeaders { get; set; }

        /// <summary>
        /// Custom SQL Filters for topic subscription , more about SQL Filters and its rules 
        /// Key: Rule Name , Value: SQL Expression 
        /// https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-sql-filter
        /// </summary>
        public List<KeyValuePair<string,string>>? SQLFilters { get; set; }
        
        public AzureServiceBusOptions ConfigureCustomProducer<T>(Action<ServiceBusProducerDescriptorBuilder<T>> configuration)
        {
            var builder = new ServiceBusProducerDescriptorBuilder<T>();
            configuration(builder);
            CustomProducers.Add(builder.Build());

            return this;
        }

        internal ICollection<IServiceBusProducerDescriptor> CustomProducers { get; set; } =
            new List<IServiceBusProducerDescriptor>();
    }
}
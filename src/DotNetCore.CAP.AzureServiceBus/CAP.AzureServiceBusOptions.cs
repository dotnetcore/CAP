// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using DotNetCore.CAP.AzureServiceBus;
using DotNetCore.CAP.AzureServiceBus.Producer;
using DotNetCore.CAP.Internal;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides programmatic configuration for the CAP Azure Service Bus project.
    /// </summary>
    public class AzureServiceBusOptions
    {
        /// <summary>
        /// Azure Service Bus Namespace connection string. Must not contain topic information.
        /// </summary>
        public string ConnectionString { get; set; } = default!;

        /// <summary>
        /// Namespace of service bus , Needs to be set when using with TokenCredential Property
        /// </summary>
        public string Namespace { get; set; } = default!;

        /// <summary>
        /// Represents the Azure Active Directory token provider for Azure Managed Service Identity integration.
        /// </summary>
        public Azure.Core.TokenCredential? TokenCredential { get; set; }

        /// <summary>
        /// The <see cref="TimeSpan"/> idle interval after which the subscription is automatically deleted.
        /// </summary>
        /// <remarks>The minimum duration is 5 minutes. Default value is <see cref="TimeSpan.MaxValue"/>.</remarks>
        public TimeSpan SubscriptionAutoDeleteOnIdle { get; set; } = TimeSpan.MaxValue;

        /// <summary>
        /// Gets the maximum number of concurrent calls to the ProcessMessageAsync message handler the processor should initiate. Default value is 1.
        /// </summary>
        public int MaxConcurrentCalls { get; set; } = 1;

        /// <summary>
        /// The maximum duration within which the lock will be renewed automatically. Default value is 5 minutes.
        /// </summary>
        public TimeSpan LockRenewalDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Use this function to write additional headers from the original ASB Message or any Custom Header, i.e. to allow compatibility with heterogeneous systems, into <see cref="CapHeader"/>
        /// </summary>
        public Func<ServiceBusReceivedMessage, IServiceProvider, List<KeyValuePair<string, string>>>? CustomHeadersBuilder { get; set; }


        public AzureServiceBusOptions ConfigureCustomProducer(string topicName)
        {
            CustomProducers.Add(new ServiceBusProducerDescriptor(topicName));
            
            return this;
        }

        internal ICollection<IServiceBusProducerDescriptor> CustomProducers { get; set; } =
            new List<IServiceBusProducerDescriptor>();
    }
}
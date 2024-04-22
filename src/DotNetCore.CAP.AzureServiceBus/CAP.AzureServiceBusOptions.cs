// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using DotNetCore.CAP.AzureServiceBus;
using DotNetCore.CAP.AzureServiceBus.Producer;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP;

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
    /// <see cref="AzureServiceBusHeaders.SessionId" /> header. Defaults to false.
    /// </summary>
    public bool EnableSessions { get; set; } = false;

    /// <summary>
    /// The name of the topic relative to the service namespace base address.
    /// </summary>
    public string TopicPath { get; set; } = DefaultTopicPath;

    /// <summary>
    /// The <see cref="TimeSpan" /> idle interval after which the subscription is automatically deleted.
    /// </summary>
    /// <remarks>The minimum duration is 5 minutes. Default value is <see cref="TimeSpan.MaxValue" />.</remarks>
    public TimeSpan SubscriptionAutoDeleteOnIdle { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// Duration of a peek lock receive. i.e., the amount of time that the message is locked by a given receiver so that
    /// no other receiver receives the same message.
    /// </summary>
    /// <remarks>Max value is 5 minutes. Default value is 60 seconds.</remarks>
    public TimeSpan SubscriptionMessageLockDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The default time to live value for the messages. This is the duration after which the message expires.
    /// </summary>
    /// <remarks>
    /// This is the default value used when <see cref="ServiceBusMessage.TimeToLive"/> is not set on a
    ///  message itself. Messages older than their TimeToLive value will expire and no longer be retained in the message store.
    ///  Subscribers will be unable to receive expired messages.
    /// Default value is <see cref="TimeSpan.MaxValue"/>.
    /// </remarks>
    public TimeSpan SubscriptionDefaultMessageTimeToLive { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// The maximum delivery count of a message before it is dead-lettered.
    /// </summary>
    /// <remarks>
    /// The delivery count is increased when a message is received in <see cref="ServiceBusReceiveMode.PeekLock"/> mode
    /// and didn't complete the message before the message lock expired.
    /// Default value is 10. Minimum value is 1.
    /// </remarks>
    public int SubscriptionMaxDeliveryCount { get; set; } = 10;

    /// <summary>
    /// Gets a value that indicates whether the processor should automatically complete messages after the message handler has
    /// completed processing.
    /// If the message handler triggers an exception, the message will not be automatically completed.
    /// </summary>
    public bool AutoCompleteMessages { get; set; } = true;

    /// <summary>
    /// Adds additional correlation properties to all correlation filters.
    /// https://learn.microsoft.com/en-us/azure/service-bus-messaging/topic-filters#correlation-filters
    /// </summary>
    public IDictionary<string, string> DefaultCorrelationHeaders { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the maximum number of concurrent calls to the ProcessMessageAsync message handler the processor should initiate.
    /// </summary>
    /// <remarks>Default values is 1.</remarks>
    public int MaxConcurrentCalls { get; set; } = 1;

    /// <summary>
    /// The maximum amount of time to wait for a message to be received for the
    ///  currently active session. After this time has elapsed, the processor will close the session
    ///  and attempt to process another session.
    /// </summary>
    /// <remarks>Not applicable when <see cref="EnableSessions"/> is false.</remarks>
    public TimeSpan? SessionIdleTimeout { get; set; }

    /// <summary>
    /// The maximum number of sessions that can be processed concurrently by the processor.
    /// </summary>
    /// <remarks>
    /// Not applicable when <see cref="EnableSessions"/> is false.
    /// The default value is 8.
    /// </remarks>
    public int MaxConcurrentSessions { get; set; } = 8;

    /// <summary>
    /// The maximum duration within which the lock will be renewed automatically.
    /// </summary>
    /// <remarks>
    /// This value should be greater than the longest message lock duration; for example, the LockDuration Property.
    /// To specify an infinite duration, use <see cref="Timeout.InfiniteTimeSpan"/>.
    /// The default value is 5 minutes.
    /// </remarks>
    public TimeSpan MaxAutoLockRenewalDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Represents the Azure Active Directory token provider for Azure Managed Service Identity integration.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }

    /// <summary>
    /// Use this function to write additional headers from the original ASB Message or any Custom Header, i.e. to allow
    /// compatibility with heterogeneous systems, into <see cref="CapHeader" />
    /// </summary>
    public Func<ServiceBusReceivedMessage, IServiceProvider, List<KeyValuePair<string, string>>>? CustomHeadersBuilder
    {
        get;
        set;
    }

    /// <summary>
    /// Custom SQL Filters for topic subscription , more about SQL Filters and its rules
    /// Key: Rule Name , Value: SQL Expression
    /// https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-sql-filter
    /// </summary>
    public List<KeyValuePair<string, string>>? SQLFilters { get; set; }

    internal ICollection<IServiceBusProducerDescriptor> CustomProducers { get; set; } =
        new List<IServiceBusProducerDescriptor>();

    public AzureServiceBusOptions ConfigureCustomProducer<T>(
        Action<ServiceBusProducerDescriptorBuilder<T>> configuration)
    {
        var builder = new ServiceBusProducerDescriptorBuilder<T>();
        configuration(builder);
        CustomProducers.Add(builder.Build());

        return this;
    }
}
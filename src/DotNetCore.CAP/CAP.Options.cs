// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using DotNetCore.CAP.Messages;

// ReSharper disable InconsistentNaming

namespace DotNetCore.CAP;

/// <summary>
/// Represents all the options you can use to configure the system.
/// </summary>
public class CapOptions
{
    public CapOptions()
    {
        SucceedMessageExpiredAfter = 24 * 3600;
        FailedMessageExpiredAfter = 15 * 24 * 3600;
        FailedRetryInterval = 60;
        FailedRetryCount = 50;
        ConsumerThreadCount = 1;
        EnablePublishParallelSend = false;
        EnableConsumerPrefetch = false;
        Extensions = new List<ICapOptionsExtension>();
        Version = "v1";
        DefaultGroupName = "cap.queue." + Assembly.GetEntryAssembly()?.GetName().Name!.ToLower();
        CollectorCleaningInterval = 300;
        UseDispatchingPerGroup = false;
        FallbackWindowLookbackSeconds = 240;
    }

    internal IList<ICapOptionsExtension> Extensions { get; }

    /// <summary>
    /// Subscriber default group name. kafka-->group name. rabbitmq --> queue name.
    /// </summary>
    public string DefaultGroupName { get; set; }

    /// <summary>
    /// Subscriber group prefix.
    /// </summary>
    public string? GroupNamePrefix { get; set; }

    /// <summary>
    /// Topic prefix.
    /// </summary>
    public string? TopicNamePrefix { get; set; }

    /// <summary>
    /// The default version of the message, configured to isolate data in the same instance. The length must not exceed 20
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Sent or received succeed message after time span of due, then the message will be deleted at due time.
    /// Default is 24*3600 seconds.
    /// </summary>
    public int SucceedMessageExpiredAfter { get; set; }

    /// <summary>
    /// Sent or received failed message after time span of due, then the message will be deleted at due time.
    /// Default is 15*24*3600 seconds.
    /// </summary>
    public int FailedMessageExpiredAfter { get; set; }

    /// <summary>
    /// Failed messages polling delay time.
    /// Default is 60 seconds.
    /// </summary>
    public int FailedRetryInterval { get; set; }

    /// <summary>
    /// We’ll invoke this call-back with message type,name,content when retry failed (send or executed) messages equals
    /// <see cref="FailedRetryCount" /> times.
    /// </summary>
    public Action<FailedInfo>? FailedThresholdCallback { get; set; }

    /// <summary>
    /// The number of message retries, the retry will stop when the threshold is reached.
    /// Default is 50 times.
    /// </summary>
    public int FailedRetryCount { get; set; }

    /// <summary>
    /// The number of consumer thread connections.
    /// Default is 1
    /// </summary>
    public int ConsumerThreadCount { get; set; }

    /// <summary>
    /// If true, the message will be prefetch to memory queue for parallel execute by .net thread pool.
    /// Default is false
    /// </summary>
    public bool EnableConsumerPrefetch { get; set; }

    /// <summary>
    /// If true, the message send task will be parallel execute by .net thread pool.
    /// Default is false
    /// </summary>
    public bool EnablePublishParallelSend { get; set; }

    /// <summary>
    /// If true then each message group will have own independent dispatching pipeline. Each pipeline use as many threads as
    /// <see cref="ConsumerThreadCount" /> value is.
    /// Default is false.
    /// </summary>
    public bool UseDispatchingPerGroup { get; set; }

    /// <summary>
    /// Configure the retry processor to pick up the backtrack time window for Scheduled or Failed status messages.
    /// Default is 240 seconds.
    /// </summary>
    public int FallbackWindowLookbackSeconds { get; set; }

    /// <summary>
    /// The interval of the collector processor deletes expired messages.
    /// Default is 300 seconds.
    /// </summary>
    public int CollectorCleaningInterval { get; set; }

    /// <summary>
    /// Configure JSON serialization settings
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; } = new();

    /// <summary>
    /// if true,cap will use only one instance to retry failure messages
    /// </summary>
    public bool UseStorageLock { get; set; }

    /// <summary>
    /// Registers an extension that will be executed when building services.
    /// </summary>
    /// <param name="extension"></param>
    public void RegisterExtension(ICapOptionsExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);

        Extensions.Add(extension);
    }
}
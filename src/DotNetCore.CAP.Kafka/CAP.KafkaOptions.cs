﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Confluent.Kafka;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP;

/// <summary>
/// Provides programmatic configuration for the CAP kafka project.
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// librdkafka configuration parameters (refer to https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md).
    /// <para>
    /// Topic configuration parameters are specified via the "default.topic.config" sub-dictionary config parameter.
    /// </para>
    /// </summary>
    public readonly Dictionary<string, string> MainConfig;

    public KafkaOptions()
    {
        MainConfig = new Dictionary<string, string>();
        RetriableErrorCodes = new List<ErrorCode>
        {
            ErrorCode.GroupLoadInProgress,
            ErrorCode.Local_Retry,
            ErrorCode.RequestTimedOut,
            ErrorCode.LeaderNotAvailable,
            ErrorCode.NotLeaderForPartition,
            ErrorCode.RebalanceInProgress,
            ErrorCode.NotCoordinatorForGroup,
            ErrorCode.NetworkException,
            ErrorCode.GroupCoordinatorNotAvailable
        };
    }

    /// <summary>
    /// Producer connection pool size, default is 10
    /// </summary>
    public int ConnectionPoolSize { get; set; } = 10;

    /// <summary>
    /// The `bootstrap.servers` item config of <see cref="MainConfig" />.
    /// <para>
    /// Initial list of brokers as a CSV list of broker host or host:port.
    /// </para>
    /// </summary>
    public string Servers { get; set; } = default!;

    /// <summary>
    /// The callback to use to refresh the auth token when it expires
    /// </summary>
    public Action<Object, string> OAuthCallback { get; set; }

    /// <summary>
    /// If you need to get offset and partition and so on.., you can use this function to write additional header into
    /// <see cref="CapHeader" />
    /// </summary>
    public Func<ConsumeResult<string, byte[]>, IServiceProvider, List<KeyValuePair<string, string>>>? CustomHeadersBuilder { get; set; }

    /// <summary>
    /// New retriable error code (refer to
    /// https://docs.confluent.io/platform/current/clients/librdkafka/html/rdkafkacpp_8h.html#a4c6b7af48c215724c323c60ea4080dbf)
    /// </summary>
    public IList<ErrorCode> RetriableErrorCodes { get; set; }

    public KafkaTopicOptions TopicOptions { get; set; } = new();
}

public class KafkaTopicOptions
{
    /// <summary>
    /// The number of partitions for the new topic
    /// </summary>
    public short NumPartitions { get; set; } = -1;

    /// <summary>
    /// The replication factor for the new topic
    /// </summary>
    public short ReplicationFactor { get; set; } = -1;
}

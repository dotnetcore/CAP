// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams;

internal class RedisConsumeMissingHeadersException(StreamEntry entry) : Exception(message: $"Redis entry [{entry.Id}] is missing Cap headers.");

internal class RedisConsumeMissingBodyException(StreamEntry entry) : Exception(message: $"Redis entry [{entry.Id}] is missing Cap body.");

internal class RedisConsumeInvalidHeadersException(StreamEntry entry, Exception ex)
    : Exception(message: $"Redis entry [{entry.Id}] has not headers that are formatted properly as Cap headers.", ex);

internal class RedisConsumeInvalidBodyException(StreamEntry entry, Exception ex) : Exception(message: $"Redis entry [{entry.Id}] has not body that is formatted properly as Cap body.", ex);
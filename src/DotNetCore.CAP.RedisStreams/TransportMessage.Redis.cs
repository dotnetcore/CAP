// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DotNetCore.CAP.Messages;
using StackExchange.Redis;

namespace DotNetCore.CAP.RedisStreams;

internal static class RedisMessage
{
    private const string Headers = "headers";
    private const string Body = "body";
    private readonly static JsonSerializerOptions JSON_OPTIONS = new(JsonSerializerDefaults.Web);

    public static NameValueEntry[] AsStreamEntries(this TransportMessage message)
    {
        return
        [
            new NameValueEntry(Headers, ToJson(message.Headers)),
            new NameValueEntry(Body, ToJson(message.Body.ToArray()))
        ];
    }

    public static TransportMessage Create(StreamEntry streamEntry, string? groupId = null)
    {
        IDictionary<string, string?> headers;
        byte[]? body;

        var streamDict = streamEntry.Values.ToDictionary(c => c.Name, c => c.Value);

        if (!streamDict.TryGetValue(Headers, out var headersRaw) || headersRaw.IsNullOrEmpty)
        {
            throw new RedisConsumeMissingHeadersException(streamEntry);
        }

        if (!streamDict.TryGetValue(Body, out var bodyRaw))
        {
            throw new RedisConsumeMissingBodyException(streamEntry);
        }

        try
        {
            headers = JsonSerializer.Deserialize<IDictionary<string, string?>>(json: headersRaw!, JSON_OPTIONS)!;
        }
        catch (Exception ex)
        {
            throw new RedisConsumeInvalidHeadersException(streamEntry, ex);
        }

        if (!bodyRaw.IsNullOrEmpty)
        {
            try
            {
                body = JsonSerializer.Deserialize<byte[]>(json: bodyRaw!, JSON_OPTIONS);
            }
            catch (Exception ex)
            {
                throw new RedisConsumeInvalidBodyException(streamEntry, ex);
            }
        }
        else body = null;

        if (!string.IsNullOrEmpty(groupId))
        {
            headers[Messages.Headers.Group] = groupId;
        }

        return new TransportMessage(headers, body);
    }

    private static RedisValue ToJson(object? obj)
    {
        if (obj == null) return RedisValue.Null;
        return JsonSerializer.Serialize(obj, JSON_OPTIONS);
    }
}
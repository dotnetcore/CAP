// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Serialization;

public class JsonUtf8Serializer : ISerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JsonUtf8Serializer(IOptions<CapOptions> capOptions)
    {
        _jsonSerializerOptions = capOptions.Value.JsonSerializerOptions;
    }

    public ValueTask<TransportMessage> SerializeAsync(Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        if (message.Value == null) return new ValueTask<TransportMessage>(new TransportMessage(message.Headers, null));

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message.Value, _jsonSerializerOptions);

        return new ValueTask<TransportMessage>(new TransportMessage(message.Headers, jsonBytes));
    }

    public ValueTask<Message> DeserializeAsync(TransportMessage transportMessage, Type? valueType)
    {
        if (valueType == null || transportMessage.Body.Length == 0)
            return new ValueTask<Message>(new Message(transportMessage.Headers, null));

        var obj = JsonSerializer.Deserialize(transportMessage.Body.Span, valueType, _jsonSerializerOptions);

        return new ValueTask<Message>(new Message(transportMessage.Headers, obj));
    }

    public string Serialize(Message message)
    {
        return JsonSerializer.Serialize(message, _jsonSerializerOptions);
    }

    public Message? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<Message>(json, _jsonSerializerOptions);
    }

    public object? Deserialize(object value, Type valueType)
    {
        if (value is JsonElement jsonElement) return jsonElement.Deserialize(valueType, _jsonSerializerOptions);

        throw new NotSupportedException("Type is not of type JsonElement");
    }

    public bool IsJsonType(object jsonObject)
    {
        return jsonObject is JsonElement;
    }
}
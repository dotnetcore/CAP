// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Serialization
{
    public class JsonUtf8Serializer : ISerializer
    {
        public Task<TransportMessage> SerializeAsync(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.Value == null)
            {
                return Task.FromResult(new TransportMessage(message.Headers, null));
            }

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message.Value);
            return Task.FromResult(new TransportMessage(message.Headers, jsonBytes));
        }

        public Task<Message> DeserializeAsync(TransportMessage transportMessage, Type valueType)
        {
            if (valueType == null || transportMessage.Body == null)
            {
                return Task.FromResult(new Message(transportMessage.Headers, null));
            }

            var obj = JsonSerializer.Deserialize(transportMessage.Body, valueType);

            return Task.FromResult(new Message(transportMessage.Headers, obj));
        }

        public string Serialize(Message message)
        {
            return JsonSerializer.Serialize(message);
        }

        public Message Deserialize(string json)
        {
            return JsonSerializer.Deserialize<Message>(json);
        }

        public object Deserialize(object value, Type valueType)
        {
            if (value is JsonElement jToken)
            {
                var bufferWriter = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8JsonWriter(bufferWriter))
                {
                    jToken.WriteTo(writer);
                }
                return JsonSerializer.Deserialize(bufferWriter.WrittenSpan, valueType);
            }
            throw new NotSupportedException("Type is not of type JToken");
        }

        public bool IsJsonType(object jsonObject)
        {
            return jsonObject is JsonElement;
        }
         
    }
}
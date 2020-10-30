// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

            var json = JsonConvert.SerializeObject(message.Value);
            return Task.FromResult(new TransportMessage(message.Headers, Encoding.UTF8.GetBytes(json)));
        }

        public Task<Message> DeserializeAsync(TransportMessage transportMessage, Type valueType)
        {
            if (valueType == null || transportMessage.Body == null)
            {
                return Task.FromResult(new Message(transportMessage.Headers, null));
            }

            var json = Encoding.UTF8.GetString(transportMessage.Body);
            return Task.FromResult(new Message(transportMessage.Headers, JsonConvert.DeserializeObject(json, valueType)));
        }

        public string Serialize(Message message)
        {
            return JsonConvert.SerializeObject(message);
        }

        public Message Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<Message>(json);
        }

        public object Deserialize(object value, Type valueType)
        {
            if (value is JToken jToken)
            {
                return jToken.ToObject(valueType);
            }
            throw new NotSupportedException("Type is not of type JToken");
        }

        public bool IsJsonType(object jsonObject)
        {
            return jsonObject is JToken;
        }
  }
}
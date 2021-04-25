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
    public class DefaultSerializer : IMessageSerializer
    {
        public Task<TransportMessage> SerializeAsync(ICapMessage message)
        {
            var message1 = (IMessage)message;

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message1.Value == null)
            {
                return Task.FromResult(new TransportMessage(message.Headers, null));
            }

            var json = JsonConvert.SerializeObject(message1.Value);
            return Task.FromResult(new TransportMessage(message.Headers, Encoding.UTF8.GetBytes(json)));
        }

        public Task<ICapMessage> DeserializeAsync(TransportMessage transportMessage)
        {
            if (transportMessage.Body == null)
            {
                return Task.FromResult<ICapMessage>(new Message(transportMessage.Headers, null));
            }

            var json = Encoding.UTF8.GetString(transportMessage.Body);

            return Task.FromResult<ICapMessage>(new Message(transportMessage.Headers, JsonConvert.DeserializeObject(json)));
        }

        public string Serialize(ICapMessage message)
        {
            return JsonConvert.SerializeObject(message);
        }

        public ICapMessage Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<Message>(json);
        }

    }
}
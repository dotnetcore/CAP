using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using System.Text.Json;

namespace DotNetCore.CAP.Serialization
{
    public class JsonUtf8Serializer : ISerializer
    {
        public Task<TransportMessage> SerializeAsync(Message message)
        {
            return Task.FromResult(new TransportMessage(message.Headers, JsonSerializer.SerializeToUtf8Bytes(message.Value)));
        }

        public Task<Message> DeserializeAsync(TransportMessage transportMessage, Type valueType)
        {
            if (valueType == null)
            {
                return Task.FromResult(new Message(transportMessage.Headers, null));
            }

            return Task.FromResult(new Message(transportMessage.Headers, JsonSerializer.Deserialize(transportMessage.Body, valueType)));
        }
    }
}

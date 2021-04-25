using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.CustomSerializer.Rabbit.PostgreSql.Domain
{
    public class HanoiSerializer: IMessageSerializer
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

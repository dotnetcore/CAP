using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.CustomSerializer.Rabbit.PostgreSql
{
    public class HanoiSerializer : IMessageSerializer
    {
        public ICapMessage Deserialize(string json)
        {
            throw new NotImplementedException();
        }

        public Task<ICapMessage> DeserializeAsync(TransportMessage transportMessage)
        {
            throw new NotImplementedException();
        }

        public string Serialize(ICapMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<TransportMessage> SerializeAsync(ICapMessage message)
        {
            throw new NotImplementedException();
        }
    }
}

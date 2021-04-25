using DotNetCore.CAP.Messages;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Serialization
{
    public interface IMessageSerializer
    {

        public ICapMessage Deserialize(string json);

        public string Serialize(ICapMessage message);

        public Task<TransportMessage> SerializeAsync(ICapMessage message);
        public Task<ICapMessage> DeserializeAsync(TransportMessage transportMessage);

    }

    public interface IMessageSerializer<T> 
    {
        public  IMessage<T> Deserialize(string json);

        public string Serialize(IMessage<T> message);

        public Task<TransportMessage> SerializeAsync(IMessage<T> message);
        public Task<IMessage<T>> DeserializeAsync(TransportMessage transportMessage);
    }


}

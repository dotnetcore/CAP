using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Serialization
{
    public class SerializerRegistry: ISerializerRegistry
    {
        private static IMessageSerializerProvider _messageSerializerProvider;

        public SerializerRegistry(
                    IMessageSerializerProvider messageSerializerProvider)
        {
            _messageSerializerProvider = messageSerializerProvider;
        }


        public void AddMessageSerializer<TValue, IMessageSerializer>()
        {
            _messageSerializerProvider.AddMessageSerializer(typeof(TValue), typeof(IMessageSerializer));
        }

        public IMessageSerializer GetMessageSerializer(Type typeOfValue)
        {
            if(typeOfValue == null)
            {
                return _messageSerializerProvider.GetSerializer();
            }

            var serializer = _messageSerializerProvider.GetSerializer(typeOfValue);

            if (serializer != null) return serializer;

            return _messageSerializerProvider.GetSerializer();

        }

        public IMessageSerializer GetMessageSerializer()
        {
            return _messageSerializerProvider.GetSerializer();
        }
    }
}

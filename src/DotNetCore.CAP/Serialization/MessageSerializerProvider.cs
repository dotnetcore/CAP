using DotNetCore.CAP.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DotNetCore.CAP.Serialization
{
    public class MessageSerializerProvider: IMessageSerializerProvider
    {
        private readonly Dictionary<Type, Type> _serializerTypes;
        private readonly ConcurrentDictionary<Type, IMessageSerializer> _cache;
        private readonly IMessageSerializer _defaultSerializer;

        public MessageSerializerProvider()
        {
            _serializerTypes = new Dictionary<Type, Type>();
            _cache = new ConcurrentDictionary<Type, IMessageSerializer>();
            _defaultSerializer = new DefaultSerializer();
        }


        public void AddMessageSerializer(Type type, Type typeOfSerializer)
        {
            if (type == null || typeOfSerializer == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _serializerTypes.Add(type, typeOfSerializer);
        }

        public IMessageSerializer CreateSerializer(Type serializerType)
        {
            var serializerTypeInfo = serializerType.GetTypeInfo();

            var constructorInfo = serializerTypeInfo.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public, null,
                CallingConventions.Standard, new Type[0], null);

            IMessageSerializer result;

            if (constructorInfo != null)
            {
                result = (IMessageSerializer)constructorInfo.Invoke(null);
                if (_cache.TryAdd(serializerType, result))
                {
                    return result;
                }
            }

            constructorInfo = serializerTypeInfo.GetConstructor(new Type[0]);

            if (constructorInfo != null)
            {
                result = (IMessageSerializer)constructorInfo.Invoke(new object[0]);

                if (_cache.TryAdd(serializerType, result))
                {
                    return result;
                }
                
            }

            throw new MissingMethodException(string.Format(
                "No suitable constructor found for serializer type: '{0}'.",
                serializerType.FullName));
        }

        public IMessageSerializer GetSerializer(Type type)
        {
            Type serializerType;
            IMessageSerializer messageSerializer;

            if(_serializerTypes.TryGetValue(type, out serializerType)){

                if (_cache.TryGetValue(serializerType, out messageSerializer))
                {
                    return messageSerializer;
                }

                return CreateSerializer(serializerType);

            }

            return null;
        }

        public IMessageSerializer GetSerializer()
        {
            if(_defaultSerializer == null)
            {
                var message = string.Format("No serializer found for type {0}.");
                throw new Exception(message);
            }
            return _defaultSerializer;
        }
    }
}

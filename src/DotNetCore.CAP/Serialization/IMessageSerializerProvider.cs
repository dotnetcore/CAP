using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Serialization
{
    public interface IMessageSerializerProvider
    {
        // <summary>
        /// Gets a serializer for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A serializer.</returns>
        IMessageSerializer GetSerializer(Type type);

        IMessageSerializer CreateSerializer(Type serializerType);

        void AddMessageSerializer(Type type, Type typeOfSerializer);


        IMessageSerializer GetSerializer();
    }
}

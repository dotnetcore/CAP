using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Serialization
{
    public interface ISerializerRegistry
    {
        public IMessageSerializer GetMessageSerializer(Type typeOfValue);

        public IMessageSerializer GetMessageSerializer();

    }

}

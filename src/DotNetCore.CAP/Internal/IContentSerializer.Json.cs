using System;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Internal
{
    internal class JsonContentSerializer : IContentSerializer
    {
        public T DeSerialize<T>(string messageObjStr) 
        {
            return Helper.FromJson<T>(messageObjStr);
        }

        public object DeSerialize(string content, Type type)
        {
            return Helper.FromJson(content, type);
        }

        public string Serialize<T>(T messageObj)
        {
            return Helper.ToJson(messageObj);
        }
    }
}
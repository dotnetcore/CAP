using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Internal
{
    public class JsonContentSerializer : IContentSerializer
    {
        public T DeSerialize<T>(string messageObjStr) 
        {
            return Helper.FromJson<T>(messageObjStr);
        }

        public string Serialize<T>(T messageObj)
        {
            return Helper.ToJson(messageObj);
        }
    }
}
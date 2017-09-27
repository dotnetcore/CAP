using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Internal
{
    public class JsonContentSerializer : IContentSerializer
    {
        public T DeSerialize<T>(string messageObjStr) where T : CapMessageDto, new() {

            return Helper.FromJson<T>(messageObjStr);
        }

        public string Serialize<T>(T messageObj) where T : CapMessageDto, new() {

            return Helper.ToJson(messageObj);
        }
    }
}

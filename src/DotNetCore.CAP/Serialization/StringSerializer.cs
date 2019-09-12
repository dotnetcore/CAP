using System;
using System.Runtime.Serialization;
using DotNetCore.CAP.Messages;
using Newtonsoft.Json;

namespace DotNetCore.CAP.Serialization
{
    public class StringSerializer
    {
        public static string Serialize(Message message)
        {
            return JsonConvert.SerializeObject(message);
        }

        public static Message DeSerialize(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Message>(json);
            }
            catch (Exception exception)
            {
                throw new SerializationException($"Could not deserialize JSON text '{json}'", exception);
            }
        }
    }
}
using DotNetCore.CAP.Messages;
using System.Text.Json;

namespace DotNetCore.CAP.Serialization
{
    public class StringSerializer
    {
        public static string Serialize(Message message)
        {
            return JsonSerializer.Serialize(message);
        }

        public static Message DeSerialize(string json)
        {
            return JsonSerializer.Deserialize<Message>(json);
        }
    }
}
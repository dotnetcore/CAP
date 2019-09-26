using System.IO;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using System.Runtime.Serialization.Formatters.Binary;

namespace DotNetCore.CAP.Serialization
{
    public class MemorySerializer : ISerializer
    {
        public Task<TransportMessage> SerializeAsync(Message message)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, message.Value);
                return Task.FromResult(new TransportMessage(message.Headers, ms.ToArray()));
            }
        }

        public async Task<Message> DeserializeAsync(TransportMessage transportMessage)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                await memStream.WriteAsync(transportMessage.Body, 0, transportMessage.Body.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return new Message(transportMessage.Headers, obj);
            }
        }
    }
}

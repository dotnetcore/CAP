using DotNetCore.CAP.Messages;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Messages = DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Redis
{
    class RedisMessage : TransportMessage
    {
        public RedisMessage(IDictionary<string, string> headers, byte[] body) : base(headers, body) { }

        public string GroupId
        {
            get => Headers.TryGetValue(Messages.Headers.Group, out var value) ? value : default;
            set => Headers.TryAdd(Messages.Headers.Group, value);
        }

        internal RedisValue AsRedisValue()
        {
            return JsonSerializer.Serialize(new RedisMessageValue
            {
                Headers = Headers,
                Body = Body
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        public static RedisMessage Create(RedisValue redisValue)
        {
            if (redisValue.IsNullOrEmpty)
                return Empty();

            var value = JsonSerializer.Deserialize<RedisMessageValue>(redisValue, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return new RedisMessage(value.Headers, value.Body);
        }

        public static RedisMessage Empty()
        {
            return new RedisMessage(new Dictionary<string, string>(), Array.Empty<byte>());
        }
    }

    class RedisMessageValue
    {
        public IDictionary<string, string> Headers { get; set; }
        public byte[] Body { get; set; }
    }
}

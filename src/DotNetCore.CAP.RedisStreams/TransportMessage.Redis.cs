using System;
using DotNetCore.CAP.Messages;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotNetCore.CAP.RedisStreams
{
    static class RedisMessage
    {
        const string HEADERS = "headers";
        const string BODY = "body";

        public static NameValueEntry[] AsStreamEntries(this TransportMessage message)
        {
            return new[]{
                new NameValueEntry(HEADERS,ToJson(message.Headers)),
                new NameValueEntry(BODY,ToJson(message.Body))
            };
        }

        public static TransportMessage Create(StreamEntry streamEntry, string groupId = null)
        {
            if (streamEntry.IsNull)
                return null;

            var headersRaw = streamEntry[HEADERS];
            if (headersRaw.IsNullOrEmpty)
                throw new ArgumentException($"Redis stream entry with id {streamEntry.Id} missing cap headers");

            var headers = JsonSerializer.Deserialize<IDictionary<string, string>>(headersRaw);
            
            var bodyRaw = streamEntry[BODY];
            
            var body = !bodyRaw.IsNullOrEmpty ? JsonSerializer.Deserialize<byte[]>(bodyRaw) : null;

            headers.TryAdd(Headers.Group, groupId);

            return new TransportMessage(headers, body);
        }

        private static string ToJson(object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

    }
}

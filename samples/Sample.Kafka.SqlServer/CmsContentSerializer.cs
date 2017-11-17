using System;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Newtonsoft.Json;

namespace Sample.RabbitMQ.SqlServer
{
    public class MessageContent : CapMessage
    {
        [JsonProperty("id")]
        public override string Id { get; set; }

        [JsonProperty("createdTime")]
        public override DateTime Timestamp { get; set; }

        [JsonProperty("msgBody")]
        public override string Content { get; set; }

        [JsonProperty("callbackTopicName")]
        public override string CallbackName { get; set; }
    }

    public class MyMessagePacker : IMessagePacker
    {
        private readonly IContentSerializer _serializer;

        public MyMessagePacker(IContentSerializer serializer)
        {
            _serializer = serializer;
        }

        public string Pack(CapMessage obj)
        {
            var content = new MessageContent
            {
                Id = obj.Id,
                Content = obj.Content,
                CallbackName = obj.CallbackName,
                Timestamp = obj.Timestamp
            };
            return _serializer.Serialize(content);
        }

        public CapMessage UnPack(string packingMessage)
        {
            return _serializer.DeSerialize<MessageContent>(packingMessage);
        }
    }
}


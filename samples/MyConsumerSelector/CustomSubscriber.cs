using System;
using DotNetCore.CAP;

namespace MyConsumerSelector
{
    public class CustomSubscriber : IMessageSubscriber, ICapSubscribe
    {
        [MessageSubscription("string")]
        public void String(string message)
        {
            Console.WriteLine($"String: {message}");
        }
        
        [MessageSubscription("message.string")]
        public void String(Message<string> message)
        {
            Console.WriteLine($"String: {System.Text.Json.JsonSerializer.Serialize(message)}");
        }
        
        [MessageSubscription("message.datetime")]
        public void Date(Message<DateTime> message, [FromCap] CapHeader header)
        {
            Console.WriteLine($"Date: {System.Text.Json.JsonSerializer.Serialize(message)}");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(header));
        }
        
        [MessageSubscription("message.bytes")]
        public void Bytes(Message<byte[]> message, [FromCap] CapHeader header)
        {
            Console.WriteLine($"Bytes: {System.Text.Json.JsonSerializer.Serialize(message)}");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(header));
        }

        [CapSubscribe("cap")]
        public void Cap(string message, [FromCap] CapHeader header)
        {
            Console.WriteLine($"Cap {message}");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(header));
        }
    }

    public class Message<T> 
    {
        public string Name { get; set; }
        public T Body { get; set; }
    }
}
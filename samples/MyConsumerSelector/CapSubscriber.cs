using System;
using DotNetCore.CAP;

namespace MyConsumerSelector
{
    public class CapSubscriber : ICapSubscribe
    {
        [CapSubscribe("#")]
        public void Wildcard(string message, [FromCap] CapHeader header)
        {
            Console.WriteLine($"Wildcard message {message}");
            Console.WriteLine("Wildcard CapHeaders {0}", System.Text.Json.JsonSerializer.Serialize(header));
        }
        
        [CapSubscribe("test")]
        public void Test(string message, [FromCap] CapHeader header)
        {
            Console.WriteLine($"Test message {message}");
            Console.WriteLine("Test CapHeaders {0}", System.Text.Json.JsonSerializer.Serialize(header));
        }
    }
}
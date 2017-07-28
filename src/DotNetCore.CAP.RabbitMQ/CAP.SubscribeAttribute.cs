using DotNetCore.CAP.Abstractions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    /// <summary>
    /// An attribute for subscribe RabbitMQ messages.
    /// </summary>
    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string name) : base(name)
        {
        }
    }
}
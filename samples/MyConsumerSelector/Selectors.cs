using System;

namespace MyConsumerSelector
{
    /// <summary>
    /// Flags the implementer as a class that subscribes to messages.
    /// </summary>
    public interface IMessageSubscriber { }

    /// <summary>
    /// Names the message being subscribed to.
    /// </summary>
    public class MessageSubscriptionAttribute : Attribute, INamedGroup
    {
        public MessageSubscriptionAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Group { get; set; }
    }
}